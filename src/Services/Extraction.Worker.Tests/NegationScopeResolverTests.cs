using Extraction.Worker.Services;
using Xunit;

namespace Extraction.Worker.Tests;

public sealed class NegationScopeResolverTests
{
    [Fact]
    public void IsNegated_HandlesScopedNegation()
    {
        var resolver = new NegationScopeResolver();
        var sentence = "No PE or pneumothorax; pneumonia present";

        var peIndex = sentence.IndexOf("PE", StringComparison.OrdinalIgnoreCase);
        var pneumoIndex = sentence.IndexOf("pneumothorax", StringComparison.OrdinalIgnoreCase);
        var pneumoniaIndex = sentence.IndexOf("pneumonia", StringComparison.OrdinalIgnoreCase);

        Assert.True(resolver.IsNegated(sentence, peIndex));
        Assert.True(resolver.IsNegated(sentence, pneumoIndex));
        Assert.False(resolver.IsNegated(sentence, pneumoniaIndex));
    }

    [Fact]
    public void IsNegated_HandlesPostNegation()
    {
        var resolver = new NegationScopeResolver();
        var sentence = "Pneumothorax not seen";

        var index = sentence.IndexOf("Pneumothorax", StringComparison.OrdinalIgnoreCase);
        Assert.True(resolver.IsNegated(sentence, index));
    }

    [Fact]
    public void IsNegated_DoesNotNegateWithoutPattern()
    {
        var resolver = new NegationScopeResolver();
        var sentence = "Fracture without displacement";

        var index = sentence.IndexOf("Fracture", StringComparison.OrdinalIgnoreCase);
        Assert.False(resolver.IsNegated(sentence, index));
    }
}
