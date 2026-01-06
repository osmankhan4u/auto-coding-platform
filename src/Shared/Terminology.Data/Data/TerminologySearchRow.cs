namespace Terminology.Data;

public sealed class TerminologySearchRow
{
    public string Code { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string LongDescription { get; set; } = string.Empty;
    public double Score { get; set; }
    public string[] MatchModes { get; set; } = Array.Empty<string>();
    public string[] MatchedTerms { get; set; } = Array.Empty<string>();
}
