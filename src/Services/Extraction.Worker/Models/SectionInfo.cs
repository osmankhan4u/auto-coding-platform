namespace Extraction.Worker.Models;

public sealed class SectionInfo
{
    public string Name { get; init; } = string.Empty;
    public int ContentStart { get; init; }
    public int ContentEnd { get; init; }
    public string ContentText { get; init; } = string.Empty;
}
