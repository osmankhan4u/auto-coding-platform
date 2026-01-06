using NpgsqlTypes;

namespace Terminology.Api.Data.Entities;

public sealed class TerminologyConcept
{
    public Guid Id { get; set; }
    public Guid CodeVersionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string LongDescription { get; set; } = string.Empty;
    public bool IsBillable { get; set; }
    public bool IsHeader { get; set; }
    public string SearchText { get; set; } = string.Empty;
    public NpgsqlTsVector? SearchTsv { get; set; }

    public TerminologyCodeVersion? CodeVersion { get; set; }
    public ICollection<TerminologyAlias> Aliases { get; set; } = new List<TerminologyAlias>();
    public ICollection<TerminologyEmbedding> Embeddings { get; set; } = new List<TerminologyEmbedding>();
}
