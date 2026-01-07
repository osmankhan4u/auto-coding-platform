using Extraction.Worker.Services;
using Xunit;

namespace Extraction.Worker.Tests;

public sealed class ModalityBodyRegionExtractorTests
{
    [Theory]
    [InlineData("CT CHEST W CONTRAST", "CT", "CHEST")]
    [InlineData("MRI BRAIN", "MRI", "BRAIN_HEAD")]
    [InlineData("US ABDOMEN", "US", "ABDOMEN")]
    [InlineData("X-RAY KNEE", "XR", "KNEE")]
    public void Extract_DetectsModalityAndBodyRegion(string text, string expectedModality, string expectedRegion)
    {
        var extractor = new ModalityBodyRegionExtractor();
        var result = extractor.Extract(text);

        Assert.Equal(expectedModality, result.Modality);
        Assert.Equal(expectedRegion, result.BodyRegion);

        if (expectedModality != "UNKNOWN")
        {
            Assert.NotEmpty(result.ModalityEvidenceSpans);
        }

        if (expectedRegion != "UNKNOWN")
        {
            Assert.NotEmpty(result.BodyRegionEvidenceSpans);
        }
    }
}
