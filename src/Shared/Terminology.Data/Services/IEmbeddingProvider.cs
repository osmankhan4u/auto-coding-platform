namespace Terminology.Data.Services;

public interface IEmbeddingProvider
{
    string ModelId { get; }

    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken);
}
