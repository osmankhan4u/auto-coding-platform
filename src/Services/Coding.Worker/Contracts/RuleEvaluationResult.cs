namespace Coding.Worker.Contracts;

public sealed class RuleEvaluationResult
{
    public string Status { get; set; } = "PASS";
    public string Severity { get; set; } = "NON_BLOCKING";
    public List<string> Actions { get; set; } = new();
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
    public string Status { get; set; } = "PASS";
    public string Severity { get; set; } = "NON_BLOCKING";
    public string Action { get; set; } = "none";
    public string Message { get; set; } = string.Empty;
    public List<string> EvidencePointers { get; set; } = new();
    public Dictionary<string, string> TriggerFacts { get; set; } = new();
}
