using Coding.Worker.Models;
using Coding.Worker.Services;
using Xunit;

namespace Coding.Worker.Tests;

public sealed class SafetyGateTests
{
    [Fact]
    public void MissingIndicationBlocksAutoPrimary()
    {
        var gate = new SafetyGate();
        var encounter = new ExtractedRadiologyEncounter
        {
            Modality = "CT",
            DocumentationCompleteness = new DocumentationCompleteness { Score = 0.95 }
        };

        var result = gate.Evaluate(encounter);

        Assert.False(result.CanAutoSelect);
        Assert.Contains("MISSING_INDICATION_OR_IMPRESSION", result.Flags);
    }

    [Fact]
    public void ImpressionAllowsAutoPrimaryWhenIndicationMissing()
    {
        var gate = new SafetyGate();
        var encounter = new ExtractedRadiologyEncounter
        {
            Modality = "CT",
            DocumentationCompleteness = new DocumentationCompleteness { Score = 0.95 },
            ImpressionConcepts = new List<RadiologyConcept>
            {
                new()
                {
                    Text = "pneumonia",
                    SourcePriority = "IMPRESSION",
                    Certainty = "CONFIRMED",
                    Polarity = "POSITIVE"
                }
            }
        };

        var result = gate.Evaluate(encounter);

        Assert.True(result.CanAutoSelect);
        Assert.DoesNotContain("MISSING_INDICATION_OR_IMPRESSION", result.Flags);
    }

    [Fact]
    public void LowDocumentationCompletenessBlocksAutoPrimary()
    {
        var gate = new SafetyGate();
        var encounter = new ExtractedRadiologyEncounter
        {
            Modality = "CT",
            IndicationText = "Abdominal pain",
            DocumentationCompleteness = new DocumentationCompleteness { Score = 0.69 }
        };

        var result = gate.Evaluate(encounter);

        Assert.False(result.CanAutoSelect);
        Assert.Contains("LOW_DOCUMENTATION_COMPLETENESS", result.Flags);
    }

    [Fact]
    public void GlobalOnlyPackFallbackBlocksAutoPrimary()
    {
        var gate = new SafetyGate();
        var encounter = new ExtractedRadiologyEncounter
        {
            Modality = "CT",
            IndicationText = "Abdominal pain",
            DocumentationCompleteness = new DocumentationCompleteness { Score = 0.95 },
            Warnings = new List<string> { "CONCEPT_PACK_FALLBACK_GLOBAL_ONLY" }
        };

        var result = gate.Evaluate(encounter);

        Assert.False(result.CanAutoSelect);
        Assert.Contains("PACK_FALLBACK_GLOBAL_ONLY", result.Flags);
    }
}
