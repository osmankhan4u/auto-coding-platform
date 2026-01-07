using Coding.Worker.Models;
using Coding.Worker.Services;
using Xunit;

namespace Coding.Worker.Tests;

public sealed class ClaimContextBuilderTests
{
    [Fact]
    public void Build_ExtractsEvidenceSnippetsFromSections()
    {
        var encounter = new ExtractedRadiologyEncounter
        {
            ReportText = "Pain",
            Sections = new Dictionary<string, string>
            {
                ["Indication"] = "Pain"
            },
            IndicationEvidenceSpans = new List<string> { "Indication:0-4" }
        };

        var builder = new ClaimContextBuilder();
        var claim = builder.Build(encounter, new Contracts.CptCodingResult(), new Contracts.RadiologyIcdCodingResult());

        Assert.Single(claim.Evidence);
        Assert.Equal("Pain", claim.Evidence[0].Snippet);
    }

    [Fact]
    public void Build_ExtractsSnippetFromReportSpan()
    {
        var encounter = new ExtractedRadiologyEncounter
        {
            ReportText = "0123456789",
            ModalityEvidenceSpans = new List<string> { "Report:2-6" }
        };

        var builder = new ClaimContextBuilder();
        var claim = builder.Build(encounter, new Contracts.CptCodingResult(), new Contracts.RadiologyIcdCodingResult());

        Assert.Single(claim.Evidence);
        Assert.Equal("2345", claim.Evidence[0].Snippet);
    }

    [Fact]
    public void Build_FallsBackToSpanWhenSectionUnavailable()
    {
        var encounter = new ExtractedRadiologyEncounter
        {
            ModalityEvidenceSpans = new List<string> { "Report:10-20" }
        };

        var builder = new ClaimContextBuilder();
        var claim = builder.Build(encounter, new Contracts.CptCodingResult(), new Contracts.RadiologyIcdCodingResult());

        Assert.Single(claim.Evidence);
        Assert.Equal("Report:10-20", claim.Evidence[0].Snippet);
    }
}
