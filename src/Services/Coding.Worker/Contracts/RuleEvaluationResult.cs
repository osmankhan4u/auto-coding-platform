namespace Coding.Worker.Contracts;

public sealed class RuleEvaluationResult
{
    public RuleStatus Status { get; set; } = RuleStatus.Pass;
    public RuleSeverity Severity { get; set; } = RuleSeverity.NonBlocking;
    public List<RuleActionType> Actions { get; set; } = new();
    public RuleOutcome? WinningRule { get; set; }
    public List<RuleOutcome> Outcomes { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}

public sealed class RuleOutcome
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleVersion { get; set; } = string.Empty;
    public string Layer { get; set; } = string.Empty;
    public int Priority { get; set; }
    public RuleCategory Category { get; set; } = RuleCategory.Integrity;
    public RuleStatus Status { get; set; } = RuleStatus.Pass;
    public RuleSeverity Severity { get; set; } = RuleSeverity.NonBlocking;
    public RuleActionType Action { get; set; } = RuleActionType.None;
    public string Message { get; set; } = string.Empty;
    public List<string> EvidencePointers { get; set; } = new();
    public Dictionary<string, string> TriggerFacts { get; set; } = new();
}

public enum RuleStatus
{
    Pass,
    Fail,
    Warn,
    NeedsInfo
}

public enum RuleSeverity
{
    NonBlocking,
    Blocking
}

public enum RuleActionType
{
    None,
    AutoRelease,
    RoutePredicted,
    RequestInfo,
    SuggestModifierChange
}

public enum RuleCategory
{
    Integrity,
    MedicalNecessity,
    NcciMue,
    Auth,
    Frequency,
    PosSpecialty,
    Modifier,
    ClientContract
}
