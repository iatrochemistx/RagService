using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RagService.Application.Interfaces;
using RagService.Domain.Models;

namespace RagService.Infrastructure.VectorSearch
{
    /// <summary>
    /// In-memory cosine-similarity search over all documents found in /data.
    /// Index is built once at startup; thread-safe because the list is
    /// read-only after the ctor completes.
    /// </summary>
    public sealed class VectorSearchService : IVectorSearchService
    {

 
   
           private readonly IEmbeddingService _embedder;
        private readonly List<(Document Doc, float[] Vec, float Norm)> _index = new();

        public VectorSearchService(IEmbeddingService embedder)
        {
            _embedder = embedder;
            LoadDocumentsAsync().GetAwaiter().GetResult();
        }

        /* ------------------  IVectorSearchService  ------------------ */

        public async Task<List<Document>> GetTopDocumentsAsync(string query, int topK = 3,CancellationToken cancellationToken =default)
        {
            var qVec = await _embedder.EmbedAsync(query);
            return GetTopDocumentsInternal(qVec, topK);
        }

        public Task<List<Document>> GetTopDocumentsAsync(float[] queryVector, int topK = 3, CancellationToken cancellationToken = default)
            => Task.FromResult(GetTopDocumentsInternal(queryVector, topK));

        /* ------------------  Internal helpers  ------------------ */

        private List<Document> GetTopDocumentsInternal(float[] qVec, int k)
        {
            var qNorm = VectorNorm(qVec);
            return _index
                .Select(t => new
                {
                    t.Doc,
                    Score = Dot(qVec, t.Vec) / (qNorm * t.Norm)     // cosine sim
                })
                .OrderByDescending(x => x.Score)
                .Take(k)
                .Select(x => x.Doc)
                .ToList();
        }

        private async Task LoadDocumentsAsync()
        {
            var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
            if (!Directory.Exists(dataDir)) return;

            var files = Directory.EnumerateFiles(dataDir, "*.*", SearchOption.TopDirectoryOnly)
                                 .Where(f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                                             f.EndsWith(".md",  StringComparison.OrdinalIgnoreCase));

            int id = 0;
            foreach (var path in files)
            {
                var text = await File.ReadAllTextAsync(path);
                var vec  = await _embedder.EmbedAsync(text);

                var doc = new Document
                {
                    Id       = id++,
                    FileName = Path.GetFileName(path),
                    Text     = text,
                    Vector   = vec
                };
                _index.Add((doc, vec, VectorNorm(vec)));
            }
        }

        private static float Dot(float[] a, float[] b)
        {
            double sum = 0;
            for (int i = 0; i < a.Length; i++)
                sum += a[i] * b[i];
            return (float)sum;
        }

        private static float VectorNorm(float[] v)
        {
            double sumSq = 0;
            for (int i = 0; i < v.Length; i++)
                sumSq += v[i] * v[i];
            return (float)Math.Sqrt(sumSq);
        }

       
    }
}
