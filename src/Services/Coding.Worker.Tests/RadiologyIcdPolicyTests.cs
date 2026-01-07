using Coding.Worker.Models;
using Coding.Worker.Services;
using Xunit;

namespace Coding.Worker.Tests;

public sealed class RadiologyIcdPolicyTests
{
    [Fact]
    public void RejectsRuledOutConceptsForPrimary()
    {
        var policy = new RadiologyIcdPolicy();
        var concept = new RadiologyConcept
        {
            SourcePriority = "INDICATION",
            Certainty = "RULED_OUT",
            Polarity = "POSITIVE"
        };

        var isEligible = policy.IsEligibleForPrimary(concept);

        Assert.False(isEligible);
    }

    [Fact]
    public void RejectsNegativeConceptsForPrimary()
    {
        var policy = new RadiologyIcdPolicy();
        var concept = new RadiologyConcept
        {
            SourcePriority = "INDICATION",
            Certainty = "CONFIRMED",
            Polarity = "NEGATIVE"
        };

        var isEligible = policy.IsEligibleForPrimary(concept);

        Assert.False(isEligible);
    }

    [Fact]
    public void RejectsNonIndicationConceptsForPrimary()
    {
        var policy = new RadiologyIcdPolicy();
        var concept = new RadiologyConcept
        {
            SourcePriority = "FINDINGS",
            Certainty = "CONFIRMED",
            Polarity = "POSITIVE"
        };

        var isEligible = policy.IsEligibleForPrimary(concept);

        Assert.False(isEligible);
    }

    [Fact]
    public void AcceptsImpressionConceptsForPrimary()
    {
        var policy = new RadiologyIcdPolicy();
        var concept = new RadiologyConcept
        {
            SourcePriority = "IMPRESSION",
            Certainty = "CONFIRMED",
            Polarity = "POSITIVE",
            Relevance = "INDICATION_RELATED"
        };

        var isEligible = policy.IsEligibleForPrimary(concept);

        Assert.True(isEligible);
    }

    [Fact]
    public void RejectsIncidentalConceptsForPrimary()
    {
        var policy = new RadiologyIcdPolicy();
        var concept = new RadiologyConcept
        {
            SourcePriority = "IMPRESSION",
            Certainty = "CONFIRMED",
            Polarity = "POSITIVE",
            Relevance = "INCIDENTAL"
        };

        var isEligible = policy.IsEligibleForPrimary(concept);

        Assert.False(isEligible);
    }
}
