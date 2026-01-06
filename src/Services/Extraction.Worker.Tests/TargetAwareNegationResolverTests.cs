using Extraction.Worker.Services;
using Xunit;

namespace Extraction.Worker.Tests;

public sealed class TargetAwareNegationResolverTests
{
    [Fact]
    public void IsNegated_DetectsNoTargetPattern()
    {
        var resolver = new TargetAwareNegationResolver();
        var sentence = "No fracture.";

        Assert.True(resolver.IsNegated(sentence, "fracture", sentence.IndexOf("fracture", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void IsNegated_DoesNotNegateWithoutPattern()
    {
        var resolver = new TargetAwareNegationResolver();
        var sentence = "Fracture without displacement.";

        Assert.False(resolver.IsNegated(sentence, "fracture", sentence.IndexOf("fracture", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void IsNegated_DetectsPostNegationPattern()
    {
        var resolver = new TargetAwareNegationResolver();
        var sentence = "Pneumothorax not seen.";

        Assert.True(resolver.IsNegated(sentence, "pneumothorax", sentence.IndexOf("pneumothorax", StringComparison.OrdinalIgnoreCase)));
    }
}
