using Extraction.Worker.Services;
using Xunit;

namespace Extraction.Worker.Tests;

public sealed class ConceptPackRegistryTests
{
    [Fact]
    public void Resolve_IncludesCtChestPatterns()
    {
        var registry = new ConceptPackRegistry();
        var resolution = registry.Resolve("CT", "CHEST");

        var normalized = resolution.Patterns.Select(pattern => pattern.Normalized).ToList();
        Assert.Contains("pulmonary embolism", normalized);
        Assert.Contains("pneumothorax", normalized);
        Assert.Contains("pneumonia", normalized);
    }

    [Fact]
    public void Resolve_IncludesCtAbdomenPatterns()
    {
        var registry = new ConceptPackRegistry();
        var resolution = registry.Resolve("CT", "ABDOMEN");

        var normalized = resolution.Patterns.Select(pattern => pattern.Normalized).ToList();
        Assert.Contains("appendicitis", normalized);
        Assert.Contains("bowel obstruction", normalized);
    }
}
