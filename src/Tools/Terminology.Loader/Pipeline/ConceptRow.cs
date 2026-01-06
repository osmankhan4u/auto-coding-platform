namespace Terminology.Loader.Pipeline;

public sealed record ConceptRow(
    string Code,
    string ShortDesc,
    string LongDesc,
    bool IsHeader,
    bool IsBillable);
