namespace Terminology.Api.Models;

public sealed class TerminologySearchRequest
{
    public string CodeSystem { get; set; } = "ICD10CM";
    public string CodeVersionId { get; set; } = string.Empty;
    public DateOnly? DateOfService { get; set; }
    public string QueryText { get; set; } = string.Empty;
    public int TopN { get; set; } = 10;
    public string? IsBillableOnly { get; set; }
    public string? ExcludeHeaders { get; set; }
}
