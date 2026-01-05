namespace Terminology.Api.Data.Entities;

public sealed class TerminologyEmbedding
{
    public Guid Id { get; set; }
    public Guid ConceptId { get; set; }
    public string Model { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();

    public TerminologyConcept? Concept { get; set; }
}
