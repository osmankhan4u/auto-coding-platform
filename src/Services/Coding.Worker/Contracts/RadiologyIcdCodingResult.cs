namespace Coding.Worker.Contracts;

public sealed class RadiologyIcdCodingResult
{
    public List<IcdCandidate> PrimaryCandidates { get; set; } = new();
    public List<IcdCandidate> SecondaryCandidates { get; set; } = new();
    public IcdFinalSelection FinalSelection { get; set; } = new();
    public List<string> SafetyFlags { get; set; } = new();
    public DecisionTrace Trace { get; set; } = new();
}
