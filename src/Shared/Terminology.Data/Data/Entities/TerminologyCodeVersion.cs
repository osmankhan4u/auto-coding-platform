namespace Terminology.Data.Entities;

public sealed class TerminologyCodeVersion
{
    public Guid Id { get; set; }
    public string CodeSystem { get; set; } = string.Empty;
    public string CodeVersionId { get; set; } = string.Empty;
    public DateOnly EffectiveDate { get; set; }
    public ICollection<TerminologyConcept> Concepts { get; set; } = new List<TerminologyConcept>();
}
