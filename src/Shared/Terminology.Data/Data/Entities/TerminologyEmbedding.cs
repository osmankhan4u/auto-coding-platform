namespace Terminology.Data.Entities;

public sealed class TerminologyEmbedding
{
    public Guid Id { get; set; }
    public Guid ConceptId { get; set; }
    public Guid CodeVersionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();

    public TerminologyConcept? Concept { get; set; }
}
