using Coding.Worker.Contracts;
using Coding.Worker.Models;
using Microsoft.Extensions.Options;

namespace Coding.Worker.Services;

public sealed class RulesEngine : IRulesEngine
{
    private static readonly Dictionary<string, int> LayerOrder = new(StringComparer.OrdinalIgnoreCase)
    {
        ["GLOBAL"] = 0,
        ["NCCI_MUE"] = 1,
        ["PAYER"] = 2,
        ["CLIENT"] = 3
    };

    private readonly RulesOptions _options;

    public RulesEngine(IOptions<RulesOptions> options)
    {
        _options = options.Value ?? new RulesOptions();
    }

    public RuleEvaluationResult Evaluate(ClaimContext claim)
    {
        var result = new RuleEvaluationResult();

        var applicablePacks = SelectRulePacks(claim).ToList();
        if (applicablePacks.Count == 0)
        {
            result.Notes.Add("No rule packs applicable.");
            return result;
        }

        var outcomes = new List<RuleOutcome>();

        foreach (var pack in applicablePacks)
        {
            foreach (var rule in pack.Rules.OrderByDescending(r => r.Priority))
            {
                if (!TriggerMatches(rule.Trigger, claim))
                {
                    continue;
                }

                var outcome = BuildOutcome(rule, claim, pack);
                outcomes.Add(outcome);
            }
        }

        if (outcomes.Count == 0)
        {
            result.Notes.Add("No rules fired.");
            return result;
        }

        result.Outcomes = outcomes;
        result.Actions = outcomes.Select(o => o.Action).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        result.WinningRule = SelectWinningRule(outcomes);
        ApplyAggregateStatus(result);

        return result;
    }

    private IEnumerable<RulePackDefinition> SelectRulePacks(ClaimContext claim)
    {
        var payerId = claim.Header.PayerId ?? "DEFAULT";
        var planId = claim.Header.PlanId ?? string.Empty;
        var state = claim.Header.State ?? string.Empty;
        var dos = claim.Header.DateOfService;

        var packs = _options.RulePacks
            .Where(pack =>
                (string.IsNullOrWhiteSpace(pack.PayerId) || string.Equals(pack.PayerId, payerId, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(pack.PlanId) || string.Equals(pack.PlanId, planId, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(pack.State) || string.Equals(pack.State, state, StringComparison.OrdinalIgnoreCase)))
            .Where(pack => IsEffectiveOn(pack, dos))
            .OrderBy(pack => LayerOrder.TryGetValue(pack.Layer, out var order) ? order : 2)
            .ThenByDescending(pack => pack.Priority);

        if (packs.Any())
        {
            return packs;
        }

        return _options.RulePacks.Where(pack =>
            string.Equals(pack.PayerId, "DEFAULT", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsEffectiveOn(RulePackDefinition pack, DateOnly? dos)
    {
        if (!dos.HasValue)
        {
            return true;
        }

        if (pack.EffectiveStart.HasValue && dos.Value < pack.EffectiveStart.Value)
        {
            return false;
        }

        if (pack.EffectiveEnd.HasValue && dos.Value > pack.EffectiveEnd.Value)
        {
            return false;
        }

        return true;
    }

    private static bool TriggerMatches(RuleTrigger trigger, ClaimContext claim)
    {
        if (trigger.CptCodes.Count > 0 && !claim.Procedures.Any(p => trigger.CptCodes.Contains(p.Code, StringComparer.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (trigger.IcdCodes.Count > 0 && !claim.Diagnoses.Any(d => trigger.IcdCodes.Contains(d.Code, StringComparer.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (trigger.IcdPrefixes.Count > 0 && !claim.Diagnoses.Any(d => trigger.IcdPrefixes.Any(prefix =>
                d.Code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))))
        {
            return false;
        }

        if (trigger.PlaceOfService.Count > 0 &&
            !trigger.PlaceOfService.Contains(claim.Header.PlaceOfService ?? string.Empty, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (trigger.RequiresDateOfService && claim.Header.DateOfService.HasValue)
        {
            return false;
        }

        if (trigger.MinAge.HasValue && (!claim.Patient.Age.HasValue || claim.Patient.Age.Value < trigger.MinAge.Value))
        {
            return false;
        }

        if (trigger.MaxAge.HasValue && (!claim.Patient.Age.HasValue || claim.Patient.Age.Value > trigger.MaxAge.Value))
        {
            return false;
        }

        if (trigger.RequiredModifiers.Count > 0 &&
            !claim.Procedures.Any(p => trigger.RequiredModifiers.All(mod =>
                p.Modifiers.Contains(mod, StringComparer.OrdinalIgnoreCase))))
        {
            return false;
        }

        if (trigger.ProviderSpecialties.Count > 0 &&
            !trigger.ProviderSpecialties.Contains(claim.Header.RenderingProviderSpecialty ?? string.Empty, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static RuleOutcome BuildOutcome(RuleDefinition rule, ClaimContext claim, RulePackDefinition pack)
    {
        var evidencePointers = new List<string>();
        if (rule.EvidenceRequirement.RequiredEvidenceSources.Count > 0)
        {
            evidencePointers.AddRange(claim.Evidence
                .Where(e => rule.EvidenceRequirement.RequiredEvidenceSources.Contains(e.Source, StringComparer.OrdinalIgnoreCase))
                .Select(e => e.EvidenceId));
        }

        var outcome = new RuleOutcome
        {
            RuleId = rule.RuleId,
            RuleVersion = rule.RuleVersion,
            Layer = pack.Layer,
            Priority = rule.Priority,
            Status = rule.Action.Status,
            Severity = rule.Action.Severity,
            Action = rule.Action.Action,
            Message = string.IsNullOrWhiteSpace(rule.Message) ? rule.EvidenceRequirement.Message : rule.Message,
            EvidencePointers = evidencePointers,
            TriggerFacts = new Dictionary<string, string>
            {
                ["PackId"] = pack.PackId,
                ["Layer"] = pack.Layer,
                ["PayerId"] = pack.PayerId,
                ["PlanId"] = pack.PlanId,
                ["State"] = pack.State
            }
        };

        return outcome;
    }

    private static RuleOutcome SelectWinningRule(List<RuleOutcome> outcomes)
    {
        return outcomes
            .OrderByDescending(o => o.Severity == "BLOCKING")
            .ThenByDescending(o => o.Status == "FAIL")
            .ThenByDescending(o => LayerOrder.TryGetValue(o.Layer, out var order) ? order : 2)
            .ThenByDescending(o => o.Priority)
            .First();
    }

    private static void ApplyAggregateStatus(RuleEvaluationResult result)
    {
        if (result.Outcomes.Any(o => o.Status == "FAIL" && o.Severity == "BLOCKING"))
        {
            result.Status = "FAIL";
            result.Severity = "BLOCKING";
            return;
        }

        if (result.Outcomes.Any(o => o.Status == "NEEDS_INFO"))
        {
            result.Status = "NEEDS_INFO";
            result.Severity = "NON_BLOCKING";
            return;
        }

        if (result.Outcomes.Any(o => o.Status == "WARN"))
        {
            result.Status = "WARN";
            result.Severity = "NON_BLOCKING";
            return;
        }

        result.Status = "PASS";
        result.Severity = "NON_BLOCKING";
    }
}
