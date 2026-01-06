using Extraction.Worker.Services;
using Xunit;

namespace Extraction.Worker.Tests;

public sealed class SentenceSplitterTests
{
    [Fact]
    public void Split_RespectsAbbreviationsAndTerminators()
    {
        var splitter = new SentenceSplitter();
        var text = "Dr. Smith reviewed. No PE; pneumonia present: follow up\nNext line.";
        var sentences = splitter.Split(text);

        Assert.Equal(5, sentences.Count);
        Assert.Equal("Dr. Smith reviewed.", sentences[0].Text);
        Assert.Equal("No PE;", sentences[1].Text);
        Assert.Equal("pneumonia present:", sentences[2].Text);
        Assert.Equal("follow up", sentences[3].Text);
        Assert.Equal("Next line.", sentences[4].Text);
    }
}
