using Terminology.Data.Services;

namespace Terminology.Loader.Services;

public sealed class FakeBackedEmbeddingProvider : IEmbeddingProvider
{
    private readonly FakeEmbeddingProvider _inner = new();

    public FakeBackedEmbeddingProvider(string modelId)
    {
        ModelId = string.IsNullOrWhiteSpace(modelId) ? _inner.ModelId : modelId;
    }

    public string ModelId { get; }

    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        return _inner.EmbedAsync(text, cancellationToken);
    }
}
