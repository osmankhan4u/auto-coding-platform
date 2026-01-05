namespace Coding.Worker.Contracts;

public sealed class IcdFinalSelection
{
    public IcdCandidate? PrimaryIcd { get; set; }
    public List<IcdCandidate> SecondaryIcds { get; set; } = new();
    public bool RequiresHumanReview { get; set; } = true;
}
