using Coding.Worker.Contracts;
using Coding.Worker.Models;

namespace Coding.Worker.Services;

public sealed class FrequencyRuleValidator : IRuleCategoryValidator
{
    private readonly IUtilizationHistoryService _historyService;

    public FrequencyRuleValidator(IUtilizationHistoryService historyService)
    {
        _historyService = historyService;
    }

    public RuleCategory Category => RuleCategory.Frequency;

    public RuleOutcome Validate(RuleDefinition rule, RuleOutcome outcome, ClaimContext claim)
    {
        var minDays = rule.Trigger.MinDaysSinceLast;
        if (!minDays.HasValue)
        {
            outcome.Status = RuleStatus.NeedsInfo;
            outcome.Severity = RuleSeverity.NonBlocking;
            outcome.Action = RuleActionType.RoutePredicted;
            if (string.IsNullOrWhiteSpace(outcome.Message))
            {
                outcome.Message = "Frequency rule requires prior utilization history.";
            }

            return outcome;
        }

        if (claim.Header.DateOfService is null)
        {
            outcome.Status = RuleStatus.NeedsInfo;
            outcome.Severity = RuleSeverity.Blocking;
            outcome.Action = RuleActionType.RequestInfo;
            outcome.Message = string.IsNullOrWhiteSpace(outcome.Message)
                ? "Date of service required to evaluate frequency."
                : outcome.Message;
            return outcome;
        }

        var procedureCode = claim.Procedures.FirstOrDefault()?.Code ?? string.Empty;
        var patientId = claim.Patient.PatientId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(procedureCode) || string.IsNullOrWhiteSpace(patientId))
        {
            outcome.Status = RuleStatus.NeedsInfo;
            outcome.Severity = RuleSeverity.NonBlocking;
            outcome.Action = RuleActionType.RoutePredicted;
            outcome.Message = string.IsNullOrWhiteSpace(outcome.Message)
                ? "Patient identifier required for frequency check."
                : outcome.Message;
            return outcome;
        }

        if (!_historyService.TryGetMostRecentProcedureDate(patientId, procedureCode, out var lastDate))
        {
            outcome.Status = RuleStatus.NeedsInfo;
            outcome.Severity = RuleSeverity.NonBlocking;
            outcome.Action = RuleActionType.RoutePredicted;
            outcome.Message = string.IsNullOrWhiteSpace(outcome.Message)
                ? "No utilization history available for frequency check."
                : outcome.Message;
            return outcome;
        }

        var days = claim.Header.DateOfService.Value.DayNumber - lastDate.DayNumber;
        if (days < minDays.Value)
        {
            outcome.Status = RuleStatus.Warn;
            outcome.Severity = RuleSeverity.NonBlocking;
            outcome.Action = RuleActionType.RoutePredicted;
            outcome.Message = string.IsNullOrWhiteSpace(outcome.Message)
                ? $"Frequency limit: last {procedureCode} was {days} days ago."
                : outcome.Message;
        }

        return outcome;
    }
}
