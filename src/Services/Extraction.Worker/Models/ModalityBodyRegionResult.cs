namespace Extraction.Worker.Models;

public sealed class ModalityBodyRegionResult
{
    public string Modality { get; init; } = "UNKNOWN";
    public string BodyRegion { get; init; } = "UNKNOWN";
    public List<string> ModalityEvidenceSpans { get; init; } = new();
    public List<string> BodyRegionEvidenceSpans { get; init; } = new();
}
