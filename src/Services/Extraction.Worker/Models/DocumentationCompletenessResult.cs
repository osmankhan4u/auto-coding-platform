namespace Extraction.Worker.Models;

public sealed class DocumentationCompletenessResult
{
    public double Score { get; init; }
    public List<string> Warnings { get; init; } = new();
}
