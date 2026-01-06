namespace Extraction.Worker.Models;

public sealed class ConceptPackResolution
{
    public List<ConceptPattern> Patterns { get; init; } = new();
    public List<string> AppliedPacks { get; init; } = new();
}
