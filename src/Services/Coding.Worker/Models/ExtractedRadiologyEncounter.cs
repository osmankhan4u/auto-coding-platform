namespace Coding.Worker.Models;

public sealed class ExtractedRadiologyEncounter
{
    public string EncounterId { get; set; } = string.Empty;
    public string? Modality { get; set; }
    public string? IndicationText { get; set; }
    public Dictionary<string, string> Sections { get; set; } = new();
    public DocumentationCompleteness DocumentationCompleteness { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<RadiologyConcept> Concepts { get; set; } = new();
}

public sealed class DocumentationCompleteness
{
    public double Score { get; set; }
}

public sealed class RadiologyConcept
{
    public string Text { get; set; } = string.Empty;
    public string Certainty { get; set; } = string.Empty;
    public string Polarity { get; set; } = string.Empty;
    public string Temporality { get; set; } = string.Empty;
    public string SourcePriority { get; set; } = string.Empty;
    public string Relevance { get; set; } = string.Empty;
    public List<string> EvidenceSpans { get; set; } = new();
}
