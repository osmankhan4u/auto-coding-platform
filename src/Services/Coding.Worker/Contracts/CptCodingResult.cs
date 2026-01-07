namespace Coding.Worker.Contracts;

public sealed class CptCodingResult
{
    public List<CptCodeSelection> PrimaryCpts { get; set; } = new();
    public List<CptCodeSelection> AddOnCpts { get; set; } = new();
    public List<string> ExclusionReasons { get; set; } = new();
    public bool RequiresHumanReview { get; set; } = true;
}

public sealed class CptCodeSelection
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Modifiers { get; set; } = new();
    public string RuleId { get; set; } = string.Empty;
    public string RuleVersion { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public List<string> EvidenceSpans { get; set; } = new();
    public List<string> ExclusionReasons { get; set; } = new();
    public string Rationale { get; set; } = string.Empty;
}
