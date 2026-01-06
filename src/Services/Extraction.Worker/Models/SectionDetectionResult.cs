namespace Extraction.Worker.Models;

public sealed class SectionDetectionResult
{
    public Dictionary<string, SectionInfo> Sections { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
}
