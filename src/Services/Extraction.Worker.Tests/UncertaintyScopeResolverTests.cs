using Extraction.Worker.Services;
using Xunit;

namespace Extraction.Worker.Tests;

public sealed class UncertaintyScopeResolverTests
{
    [Theory]
    [InlineData("Findings suggest pneumonia.", "pneumonia")]
    [InlineData("Cannot exclude appendicitis.", "appendicitis")]
    [InlineData("Likely cyst.", "cyst")]
    public void IsUncertain_DetectsUncertaintyCues(string sentence, string target)
    {
        var resolver = new UncertaintyScopeResolver();
        var index = sentence.IndexOf(target, StringComparison.OrdinalIgnoreCase);

        Assert.True(resolver.IsUncertain(sentence, index));
    }

    [Fact]
    public void IsUncertain_IgnoresWhenNoCue()
    {
        var resolver = new UncertaintyScopeResolver();
        var sentence = "No pneumonia.";

        var index = sentence.IndexOf("pneumonia", StringComparison.OrdinalIgnoreCase);
        Assert.False(resolver.IsUncertain(sentence, index));
    }
}
