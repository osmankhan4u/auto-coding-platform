using Extraction.Worker.Services;
using Xunit;

namespace Extraction.Worker.Tests;

public sealed class HistoryScopeResolverTests
{
    [Theory]
    [InlineData("History of CVA.", "CVA")]
    [InlineData("Chronic microvascular changes.", "changes")]
    [InlineData("Prior fracture noted.", "fracture")]
    public void IsHistorical_DetectsHistoryCues(string sentence, string target)
    {
        var resolver = new HistoryScopeResolver();
        var index = sentence.IndexOf(target, StringComparison.OrdinalIgnoreCase);

        Assert.True(resolver.IsHistorical(sentence, index));
    }

    [Fact]
    public void IsHistorical_IgnoresWhenNoCue()
    {
        var resolver = new HistoryScopeResolver();
        var sentence = "Acute fracture present.";

        var index = sentence.IndexOf("fracture", StringComparison.OrdinalIgnoreCase);
        Assert.False(resolver.IsHistorical(sentence, index));
    }
}
