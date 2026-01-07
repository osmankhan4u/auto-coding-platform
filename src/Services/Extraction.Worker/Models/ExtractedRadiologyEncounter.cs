namespace Extraction.Worker.Models;

public sealed class ExtractedRadiologyEncounter
{
    public string EncounterId { get; set; } = string.Empty;
    public string PayerId { get; set; } = "DEFAULT";
    public DateOnly? DateOfService { get; set; }
    public string? Modality { get; set; }
    public string? BodyRegion { get; set; }
    public List<string> BodyRegions { get; set; } = new();
    public string? Laterality { get; set; }
    public string? ContrastState { get; set; }
    public string? ViewsOrCompleteness { get; set; }
    public bool GuidanceFlag { get; set; }
    public bool InterventionFlag { get; set; }
    public string BillingContext { get; set; } = "GLOBAL";
    public string? IndicationText { get; set; }
    public List<string> IndicationEvidenceSpans { get; set; } = new();
    public Dictionary<string, string> Sections { get; set; } = new();
    public DocumentationCompleteness DocumentationCompleteness { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<RadiologyConcept> Concepts { get; set; } = new();
    public List<RadiologyConcept> ImpressionConcepts { get; set; } = new();
    public List<string> ModalityEvidenceSpans { get; set; } = new();
    public List<string> BodyRegionEvidenceSpans { get; set; } = new();
    public List<string> LateralityEvidenceSpans { get; set; } = new();
    public List<string> ContrastEvidenceSpans { get; set; } = new();
    public List<string> ViewsOrCompletenessEvidenceSpans { get; set; } = new();
    public List<string> GuidanceEvidenceSpans { get; set; } = new();
    public List<string> InterventionEvidenceSpans { get; set; } = new();
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
