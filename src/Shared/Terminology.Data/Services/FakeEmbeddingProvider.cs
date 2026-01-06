using System.Security.Cryptography;
using System.Text;

namespace Terminology.Data.Services;

public sealed class FakeEmbeddingProvider : IEmbeddingProvider
{
    private const int EmbeddingDimensions = 1536;
    private static readonly byte[] EmptyHash = SHA256.HashData(Array.Empty<byte>());

    public string ModelId => "fake-embed-1536";

    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalized = text ?? string.Empty;
        var hash = normalized.Length == 0
            ? EmptyHash
            : SHA256.HashData(Encoding.UTF8.GetBytes(normalized));

        var vector = new float[EmbeddingDimensions];
        for (var i = 0; i < vector.Length; i++)
        {
            var b = hash[i % hash.Length];
            vector[i] = (b / 255f) * 2f - 1f;
        }

        return Task.FromResult(vector);
    }
}
