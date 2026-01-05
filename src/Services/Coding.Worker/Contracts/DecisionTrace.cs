namespace Coding.Worker.Contracts;

public sealed class DecisionTrace
{
    public List<string> PolicyDecisions { get; set; } = new();
    public List<TerminologyQueryTrace> TerminologyQueries { get; set; } = new();
}

public sealed class TerminologyQueryTrace
{
    public string QueryText { get; set; } = string.Empty;
    public int TopN { get; set; }
    public int ResultCount { get; set; }
}
