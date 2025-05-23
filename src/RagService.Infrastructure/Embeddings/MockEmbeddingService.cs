using System.Security.Cryptography;
using RagService.Application.Interfaces;


namespace RagService.Infrastructure.Embeddings;

public sealed class MockEmbeddingService : IEmbeddingService
{
    private const int Dim = 384;

       public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text));
        int seed = BitConverter.ToInt32(hash, 0);

        var rng = new Random(seed);
        var vec = new float[Dim];
        for (int i = 0; i < Dim; i++)
            vec[i] = (float)(rng.NextDouble() * 2.0 - 1.0);

        return Task.FromResult(vec);
    }   
}
