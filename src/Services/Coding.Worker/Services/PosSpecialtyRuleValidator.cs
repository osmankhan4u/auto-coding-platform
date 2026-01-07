using Coding.Worker.Contracts;
using Coding.Worker.Models;

namespace Coding.Worker.Services;

public sealed class PosSpecialtyRuleValidator : IRuleCategoryValidator
{
    public RuleCategory Category => RuleCategory.PosSpecialty;

    public RuleOutcome Validate(RuleDefinition rule, RuleOutcome outcome, ClaimContext claim)
    {
        if (rule.Trigger.PlaceOfService.Count > 0 && string.IsNullOrWhiteSpace(claim.Header.PlaceOfService))
        {
            outcome.Status = RuleStatus.NeedsInfo;
            outcome.Severity = RuleSeverity.Blocking;
            outcome.Action = RuleActionType.RequestInfo;
            outcome.Message = string.IsNullOrWhiteSpace(outcome.Message)
                ? "Place of service required for POS/specialty rule."
                : outcome.Message;
        }

        if (rule.Trigger.ProviderSpecialties.Count > 0 && string.IsNullOrWhiteSpace(claim.Header.RenderingProviderSpecialty))
        {
            outcome.Status = RuleStatus.NeedsInfo;
            outcome.Severity = RuleSeverity.Blocking;
            outcome.Action = RuleActionType.RequestInfo;
            outcome.Message = string.IsNullOrWhiteSpace(outcome.Message)
                ? "Rendering provider specialty required for POS/specialty rule."
                : outcome.Message;
        }

        return outcome;
    }
}
