using Extraction.Worker.Services;
using Xunit;

namespace Extraction.Worker.Tests;

public sealed class DocumentationCompletenessTests
{
    [Fact]
    public void Evaluate_MissingIndicationAddsWarningAndPenalizesScore()
    {
        var detector = new SectionDetector();
        var scorer = new DocumentationCompletenessScorer();
        var report = "TECHNIQUE: CT CHEST\nFINDINGS: No PE.\nIMPRESSION: Normal.";

        var sections = detector.Detect(report).Sections;
        var result = scorer.Evaluate(report, sections);

        Assert.Contains("MISSING_INDICATION_SECTION", result.Warnings);
        Assert.True(result.Score < 1.0);
    }
}
