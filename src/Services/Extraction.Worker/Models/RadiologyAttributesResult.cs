namespace Extraction.Worker.Models;

public sealed class RadiologyAttributesResult
{
    public List<string> BodyRegions { get; init; } = new();
    public string Laterality { get; init; } = "NONE";
    public string ContrastState { get; init; } = "UNKNOWN";
    public string ViewsOrCompleteness { get; init; } = "UNKNOWN";
    public bool GuidanceFlag { get; init; }
    public bool InterventionFlag { get; init; }
    public List<string> BodyRegionEvidenceSpans { get; init; } = new();
    public List<string> LateralityEvidenceSpans { get; init; } = new();
    public List<string> ContrastEvidenceSpans { get; init; } = new();
    public List<string> ViewsOrCompletenessEvidenceSpans { get; init; } = new();
    public List<string> GuidanceEvidenceSpans { get; init; } = new();
    public List<string> InterventionEvidenceSpans { get; init; } = new();
}
