using Coding.Worker.Models;

namespace Coding.Worker.Services;

public sealed class RadiologyIcdPolicy
{
    public bool IsEligibleForPrimary(RadiologyConcept concept)
    {
        if (!IsIndicationConcept(concept))
        {
            return false;
        }

        return !IsExcludedConcept(concept);
    }

    private static bool IsIndicationConcept(RadiologyConcept concept) =>
        string.Equals(concept.SourcePriority, "INDICATION", StringComparison.OrdinalIgnoreCase);

    private static bool IsExcludedConcept(RadiologyConcept concept) =>
        string.Equals(concept.Certainty, "RULED_OUT", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(concept.Polarity, "NEGATIVE", StringComparison.OrdinalIgnoreCase);
}
