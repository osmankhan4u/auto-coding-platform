namespace Terminology.Data.Entities;

public sealed class TerminologyAlias
{
    public Guid Id { get; set; }
    public Guid ConceptId { get; set; }
    public Guid CodeVersionId { get; set; }
    public string ConceptCode { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string AliasNorm { get; set; } = string.Empty;
    public decimal Weight { get; set; }

    public TerminologyConcept? Concept { get; set; }
}
