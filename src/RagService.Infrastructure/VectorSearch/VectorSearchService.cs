using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using RagService.Application.Interfaces;
using RagService.Domain.Models;

namespace RagService.Infrastructure.VectorSearch
{
    /// <summary>
    /// In-memory cosine-similarity search over all documents in {ContentRootPath}/data.
    /// </summary>
    public sealed class VectorSearchService : IVectorSearchService
    {
        private readonly IEmbeddingService _embedder;
        private readonly List<(Document Doc, float[] Vec, float Norm)> _index;

        public VectorSearchService(IEmbeddingService embedder, IHostEnvironment env)
        {
            _embedder = embedder ?? throw new ArgumentNullException(nameof(embedder));
            _index = new List<(Document, float[], float)>();

            // Resolve the 'data' folder using the host environment's content root
            var dataFolder = Path.Combine(env.ContentRootPath, "data");
            if (!Directory.Exists(dataFolder))
                throw new DirectoryNotFoundException($"Data folder not found: {dataFolder}");

            // Load and index each .txt file
            foreach (var file in Directory.GetFiles(dataFolder, "*.txt"))
            {
                var text = File.ReadAllText(file);
                // Synchronously embed text for startup
                var vector = _embedder.EmbedAsync(text).GetAwaiter().GetResult();
                var norm = VectorNorm(vector);
                var doc = new Document
                {
                    FileName = Path.GetFileName(file),
                    Text = text
                };
                _index.Add((doc, vector, norm));
            }
        }

        /// <inheritdoc/>
        public async Task<List<Document>> GetTopDocumentsAsync(string query, int topK = 3, CancellationToken cancellationToken = default)
        {
            var qVec = await _embedder.EmbedAsync(query).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return GetTopDocuments(qVec, topK);
        }

        /// <inheritdoc/>
        public async Task<List<Document>> GetTopDocumentsAsync(float[] queryVector, int topK = 3, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await Task.FromResult(GetTopDocuments(queryVector, topK));
        }

        /// <summary>
        /// Computes top-K documents by cosine similarity.
        /// </summary>
        private List<Document> GetTopDocuments(float[] qVec, int topK)
        {
            var qNorm = VectorNorm(qVec);
            return _index
                .Select(item =>
                {
                    var sim = DotProduct(qVec, item.Vec) / (qNorm * item.Norm);
                    return (item.Doc, sim);
                })
                .OrderByDescending(x => x.sim)
                .Take(topK)
                .Select(x => x.Doc)
                .ToList();
        }

        private static float DotProduct(float[] a, float[] b)
        {
            float sum = 0;
            for (int i = 0; i < a.Length; i++)
                sum += a[i] * b[i];
            return sum;
        }

        private static float VectorNorm(float[] v)
        {
            float sumSq = 0;
            foreach (var x in v)
                sumSq += x * x;
            return MathF.Sqrt(sumSq);
        }
    }
}
