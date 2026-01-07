using Coding.Worker.Models;

namespace Coding.Worker.Services;

public sealed class RadiologyIcdPolicy
{
    public bool IsEligibleForPrimary(RadiologyConcept concept)
    {
        if (!IsIndicationConcept(concept) && !IsImpressionConcept(concept))
        {
            return false;
        }

        return !IsExcludedConcept(concept) && !IsIncidentalConcept(concept);
    }

    public int GetPrimaryPriority(RadiologyConcept concept)
    {
        if (IsImpressionConcept(concept))
        {
            return 0;
        }

        if (IsIndicationConcept(concept))
        {
            return 1;
        }

        return 2;
    }

    public bool IsEligibleForSecondary(RadiologyConcept concept) =>
        !IsExcludedConcept(concept) && !IsIncidentalConcept(concept);

    private static bool IsIndicationConcept(RadiologyConcept concept) =>
        string.Equals(concept.SourcePriority, "INDICATION", StringComparison.OrdinalIgnoreCase);

    private static bool IsImpressionConcept(RadiologyConcept concept) =>
        string.Equals(concept.SourcePriority, "IMPRESSION", StringComparison.OrdinalIgnoreCase);

    private static bool IsIncidentalConcept(RadiologyConcept concept) =>
        string.Equals(concept.Relevance, "INCIDENTAL", StringComparison.OrdinalIgnoreCase);

    private static bool IsExcludedConcept(RadiologyConcept concept) =>
        string.Equals(concept.Certainty, "RULED_OUT", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(concept.Polarity, "NEGATIVE", StringComparison.OrdinalIgnoreCase);
}
