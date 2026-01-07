using Coding.Worker.Contracts;
using Coding.Worker.Models;

namespace Coding.Worker.Services;

public sealed class AuthRuleValidator : IRuleCategoryValidator
{
    public RuleCategory Category => RuleCategory.Auth;

    public RuleOutcome Validate(RuleDefinition rule, RuleOutcome outcome, ClaimContext claim)
    {
        if (rule.EvidenceRequirement.RequiredEvidenceSources.Count == 0)
        {
            return outcome;
        }

        if (outcome.EvidencePointers.Count == 0 || outcome.EvidencePointers.Contains("MISSING_REQUIRED_EVIDENCE"))
        {
            outcome.Status = RuleStatus.NeedsInfo;
            outcome.Severity = RuleSeverity.Blocking;
            outcome.Action = RuleActionType.RequestInfo;
            if (string.IsNullOrWhiteSpace(outcome.Message))
            {
                outcome.Message = "Authorization evidence required.";
            }
        }

        return outcome;
    }
}
