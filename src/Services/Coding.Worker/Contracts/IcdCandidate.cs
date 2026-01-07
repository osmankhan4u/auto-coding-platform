namespace Coding.Worker.Contracts;

public sealed class IcdCandidate
{
    public string Code { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string LongDescription { get; set; } = string.Empty;
    public double Score { get; set; }
    public double Confidence { get; set; }
    public string RuleId { get; set; } = string.Empty;
    public string RuleVersion { get; set; } = string.Empty;
    public List<string> MatchModes { get; set; } = new();
    public List<string> MatchedTerms { get; set; } = new();
    public List<string> EvidenceSpans { get; set; } = new();
    public List<string> ExclusionReasons { get; set; } = new();
    public string Rationale { get; set; } = string.Empty;
}
