namespace Terminology.Api.Data.Entities;

public sealed class TerminologyAlias
{
    public Guid Id { get; set; }
    public Guid ConceptId { get; set; }
    public string Alias { get; set; } = string.Empty;
    public string AliasNorm { get; set; } = string.Empty;

    public TerminologyConcept? Concept { get; set; }
}
