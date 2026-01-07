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
    private readonly Dictionary<RuleCategory, IRuleCategoryValidator> _validators;

    public RulesEngine(IOptions<RulesOptions> options, IEnumerable<IRuleCategoryValidator> validators)
    {
        _options = options.Value ?? new RulesOptions();
        RulesOptionsValidator.ValidateOrThrow(_options);
        _validators = validators.ToDictionary(v => v.Category);
    }

    public RuleEvaluationResult Evaluate(ClaimContext claim)
    {
        var result = new RuleEvaluationResult();
        var preflightOutcomes = BuildPreflightOutcomes(claim);
        if (preflightOutcomes.Count > 0)
        {
            result.Outcomes.AddRange(preflightOutcomes);
        }

        var selection = SelectRulePacks(claim);
        if (!string.IsNullOrWhiteSpace(selection.Note))
        {
            result.Notes.Add(selection.Note);
        }

        var applicablePacks = selection.Packs.ToList();
        if (applicablePacks.Count == 0)
        {
            result.Notes.Add("No rule packs applicable.");
            ApplyAggregateStatus(result);
            return result;
        }

        var outcomes = new List<RuleOutcome>();

        foreach (var pack in applicablePacks)
        {
            foreach (var rule in pack.Rules.OrderByDescending(r => r.Priority))
            {
                if (!TriggerMatches(rule, claim))
                {
                    continue;
                }

                var outcome = BuildOutcome(rule, claim, pack);
                if (_validators.TryGetValue(rule.Category, out var validator))
                {
                    outcome = validator.Validate(rule, outcome, claim);
                }
                outcomes.Add(outcome);
            }
        }

        if (outcomes.Count == 0)
        {
            result.Notes.Add("No rules fired.");
            ApplyAggregateStatus(result);
            return result;
        }

        result.Outcomes.AddRange(outcomes);
        result.Actions = result.Outcomes.Select(o => o.Action).Distinct().ToList();
        result.WinningRule = SelectWinningRule(result.Outcomes);
        ApplyAggregateStatus(result);

        return result;
    }

    private (IEnumerable<RulePackDefinition> Packs, string Note) SelectRulePacks(ClaimContext claim)
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
            return (packs, string.Empty);
        }

        var fallback = _options.RulePacks
            .Where(pack => string.Equals(pack.PayerId, "DEFAULT", StringComparison.OrdinalIgnoreCase))
            .Where(pack => IsEffectiveOn(pack, dos));

        if (fallback.Any())
        {
            return (fallback, "No payer-specific rule packs matched; using DEFAULT rules.");
        }

        return (Enumerable.Empty<RulePackDefinition>(), string.Empty);
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

    private static bool TriggerMatches(RuleDefinition rule, ClaimContext claim)
    {
        var trigger = rule.Trigger;
        if (trigger.CptCodes.Count > 0 && !claim.Procedures.Any(p => trigger.CptCodes.Contains(p.Code, StringComparer.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (trigger.IcdCodes.Count > 0 && !claim.Diagnoses.Any(d => trigger.IcdCodes.Contains(d.Code, StringComparer.OrdinalIgnoreCase)))
        {
            return false;
        }

        var icdPrefixMatch = trigger.IcdPrefixes.Count > 0 &&
                             claim.Diagnoses.Any(d => trigger.IcdPrefixes.Any(prefix =>
                                 d.Code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));

        if (trigger.RequiresIcdMismatch)
        {
            if (icdPrefixMatch)
            {
                return false;
            }
        }
        else if (trigger.IcdPrefixes.Count > 0 && !icdPrefixMatch)
        {
            return false;
        }

        if (trigger.PlaceOfService.Count > 0 &&
            !(rule.Category == RuleCategory.PosSpecialty && string.IsNullOrWhiteSpace(claim.Header.PlaceOfService)) &&
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
            !(rule.Category == RuleCategory.PosSpecialty && string.IsNullOrWhiteSpace(claim.Header.RenderingProviderSpecialty)) &&
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

        if (rule.EvidenceRequirement.RequiredEvidenceSources.Count > 0 && evidencePointers.Count == 0)
        {
            evidencePointers.Add("MISSING_REQUIRED_EVIDENCE");
        }

        var outcome = new RuleOutcome
        {
            RuleId = rule.RuleId,
            RuleVersion = rule.RuleVersion,
            Layer = pack.Layer,
            Priority = rule.Priority,
            Category = rule.Category,
            Status = rule.Action.Status,
            Severity = rule.Action.Severity,
            Action = rule.Action.Action,
            Message = string.IsNullOrWhiteSpace(rule.Message) ? rule.EvidenceRequirement.Message : rule.Message,
            EvidencePointers = evidencePointers,
            TriggerFacts = new Dictionary<string, string>
            {
                ["PackId"] = pack.PackId,
                ["Layer"] = pack.Layer,
                ["Category"] = rule.Category.ToString(),
                ["PayerId"] = pack.PayerId,
                ["PlanId"] = pack.PlanId,
                ["State"] = pack.State,
                ["MatchedCptCodes"] = string.Join(",", claim.Procedures
                    .Where(p => rule.Trigger.CptCodes.Contains(p.Code, StringComparer.OrdinalIgnoreCase))
                    .Select(p => p.Code)),
                ["MatchedIcdCodes"] = string.Join(",", claim.Diagnoses
                    .Where(d => rule.Trigger.IcdCodes.Contains(d.Code, StringComparer.OrdinalIgnoreCase))
                    .Select(d => d.Code)),
                ["MatchedIcdPrefixes"] = string.Join(",", claim.Diagnoses
                    .Where(d => rule.Trigger.IcdPrefixes.Any(prefix =>
                        d.Code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                    .Select(d => d.Code)),
                ["PlaceOfService"] = claim.Header.PlaceOfService
            }
        };

        return outcome;
    }

    private static RuleOutcome SelectWinningRule(List<RuleOutcome> outcomes)
    {
        return outcomes
            .OrderByDescending(o => GetStatusOrder(o.Status))
            .ThenByDescending(o => o.Severity == RuleSeverity.Blocking)
            .ThenByDescending(o => LayerOrder.TryGetValue(o.Layer, out var order) ? order : 2)
            .ThenByDescending(o => o.Priority)
            .First();
    }

    private static void ApplyAggregateStatus(RuleEvaluationResult result)
    {
        if (result.Outcomes.Any(o => o.Status == RuleStatus.Fail && o.Severity == RuleSeverity.Blocking))
        {
            result.Status = RuleStatus.Fail;
            result.Severity = RuleSeverity.Blocking;
            return;
        }

        if (result.Outcomes.Any(o => o.Status == RuleStatus.NeedsInfo))
        {
            result.Status = RuleStatus.NeedsInfo;
            result.Severity = RuleSeverity.NonBlocking;
            return;
        }

        if (result.Outcomes.Any(o => o.Status == RuleStatus.Warn))
        {
            result.Status = RuleStatus.Warn;
            result.Severity = RuleSeverity.NonBlocking;
            return;
        }

        result.Status = RuleStatus.Pass;
        result.Severity = RuleSeverity.NonBlocking;
    }

    private static int GetStatusOrder(RuleStatus status)
    {
        return status switch
        {
            RuleStatus.Fail => 3,
            RuleStatus.NeedsInfo => 2,
            RuleStatus.Warn => 1,
            _ => 0
        };
    }

    private static List<RuleOutcome> BuildPreflightOutcomes(ClaimContext claim)
    {
        var outcomes = new List<RuleOutcome>();

        if (!claim.Header.DateOfService.HasValue)
        {
            outcomes.Add(new RuleOutcome
            {
                RuleId = "GLOBAL_MISSING_DOS",
                RuleVersion = "1.0",
                Layer = "GLOBAL",
                Priority = 100,
                Status = RuleStatus.NeedsInfo,
                Severity = RuleSeverity.Blocking,
                Action = RuleActionType.RequestInfo,
                Message = "Date of service is required before claim release."
            });
        }

        if (string.IsNullOrWhiteSpace(claim.Header.PayerId))
        {
            outcomes.Add(new RuleOutcome
            {
                RuleId = "GLOBAL_MISSING_PAYER",
                RuleVersion = "1.0",
                Layer = "GLOBAL",
                Priority = 95,
                Status = RuleStatus.NeedsInfo,
                Severity = RuleSeverity.Blocking,
                Action = RuleActionType.RequestInfo,
                Message = "Payer ID is required before claim release."
            });
        }

        if (string.IsNullOrWhiteSpace(claim.Header.PlaceOfService))
        {
            outcomes.Add(new RuleOutcome
            {
                RuleId = "GLOBAL_MISSING_POS",
                RuleVersion = "1.0",
                Layer = "GLOBAL",
                Priority = 90,
                Status = RuleStatus.NeedsInfo,
                Severity = RuleSeverity.Blocking,
                Action = RuleActionType.RequestInfo,
                Message = "Place of service is required before claim release."
            });
        }

        if (string.IsNullOrWhiteSpace(claim.Header.RenderingProviderNpi))
        {
            outcomes.Add(new RuleOutcome
            {
                RuleId = "GLOBAL_MISSING_RENDERING_NPI",
                RuleVersion = "1.0",
                Layer = "GLOBAL",
                Priority = 85,
                Status = RuleStatus.NeedsInfo,
                Severity = RuleSeverity.Blocking,
                Action = RuleActionType.RequestInfo,
                Message = "Rendering provider NPI is required before claim release."
            });
        }

        if (string.IsNullOrWhiteSpace(claim.Header.BillingProviderNpi))
        {
            outcomes.Add(new RuleOutcome
            {
                RuleId = "GLOBAL_MISSING_BILLING_NPI",
                RuleVersion = "1.0",
                Layer = "GLOBAL",
                Priority = 80,
                Status = RuleStatus.NeedsInfo,
                Severity = RuleSeverity.Blocking,
                Action = RuleActionType.RequestInfo,
                Message = "Billing provider NPI is required before claim release."
            });
        }

        foreach (var procedure in claim.Procedures)
        {
            foreach (var modifier in procedure.Modifiers)
            {
                if (string.IsNullOrWhiteSpace(modifier) || modifier.Length != 2)
                {
                    outcomes.Add(new RuleOutcome
                    {
                        RuleId = "GLOBAL_INVALID_MODIFIER",
                        RuleVersion = "1.0",
                        Layer = "GLOBAL",
                        Priority = 70,
                        Status = RuleStatus.Warn,
                        Severity = RuleSeverity.NonBlocking,
                        Action = RuleActionType.RequestInfo,
                        Message = $"Modifier '{modifier}' has an invalid format."
                    });
                }
            }
        }

        return outcomes;
    }
}
