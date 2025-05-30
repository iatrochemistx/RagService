using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using RagService.Application.Interfaces;
using RagService.Domain.Models;

namespace RagService.Infrastructure.VectorSearch
{
    /// <summary>
    /// In-memory cosine-similarity search over all documents in {ContentRootPath}/data.
    /// Index initialised on first use; resilient to OpenAI hiccups.
    /// </summary>
    public sealed class VectorSearchService : IVectorSearchService
    {
        private readonly IEmbeddingService _embedder;
        private readonly ILogger<VectorSearchService> _log;
        private readonly string _dataFolder;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private readonly AsyncCircuitBreakerPolicy _breaker;

        private List<(Document Doc, float[] Vec, float Norm)>? _index;

        public VectorSearchService(
            IEmbeddingService embedder,
            IHostEnvironment env,
            ILogger<VectorSearchService> logger)
        {
            _embedder = embedder ?? throw new ArgumentNullException(nameof(embedder));
            _log      = logger   ?? throw new ArgumentNullException(nameof(logger));

            _dataFolder = Path.Combine(env.ContentRootPath, "data");
            if (!Directory.Exists(_dataFolder))
            {
                _log.LogWarning("Data folder '{Folder}' not found – continuing with empty index", _dataFolder);
            }

            // Trips for 30 s after 5 consecutive failures
            _breaker = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30),
                    (ex, ts) => _log.LogWarning(ex,
                        "Embedding circuit OPEN for {CoolingPeriod}s", ts.TotalSeconds),
                    ()    => _log.LogInformation("Embedding circuit CLOSED"));
        }

        /* ---------------------------------------------------------- API */

        public Task<List<Document>> GetTopDocumentsAsync(
            string query, int topK = 1, CancellationToken ct = default) =>
            ExecuteAsync(() => _embedder.EmbedAsync(query, ct), topK, ct);

        public Task<List<Document>> GetTopDocumentsAsync(
            float[] queryVector, int topK = 1, CancellationToken ct = default) =>
            ExecuteAsync(() => Task.FromResult(queryVector), topK, ct);

        /* ------------------------------------------------------ Internals */

        private async Task<List<Document>> ExecuteAsync(
            Func<Task<float[]>> vecFactory, int k, CancellationToken ct)
        {
            await EnsureIndexAsync(ct);

            if (_index is null || _index.Count == 0)
                return new(); // Nothing to search

            var sw = Stopwatch.StartNew();
            try
            {
                var qVec = await _breaker.ExecuteAsync(vecFactory);
                var docs = ComputeTopDocuments(qVec, k);
                _log.LogInformation("Search finished in {Elapsed} ms, topK={K}", sw.ElapsedMilliseconds, k);
                return docs;
            }
            catch (BrokenCircuitException)
            {
                _log.LogWarning("Search skipped because circuit is OPEN");
                return new();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unexpected failure while computing search results");
                throw; 
            }
        }

        /* ------------------------ one-time index build (lazy) */

        private async Task EnsureIndexAsync(CancellationToken ct)
        {
            if (_index is not null) return;

            await _initLock.WaitAsync(ct);
            try
            {
                if (_index is not null) return;

                var sw = Stopwatch.StartNew();
                var docsOnDisk = Directory.Exists(_dataFolder)
                                 ? LoadDocumentsFromDisk().ToList()
                                 : new List<(Document, string)>();

                _log.LogInformation("Index initialisation: {Count} file(s) found", docsOnDisk.Count);

                var entries = new List<(Document, float[], float)>();
                foreach (var (doc, text) in docsOnDisk)
                {
                    try
                    {
                        var vec = await _breaker.ExecuteAsync(
                            () => _embedder.EmbedAsync(text, ct));
                        entries.Add((doc, vec, ComputeNorm(vec)));
                        _log.LogDebug("Embedded {File}", doc.FileName);
                    }
                    catch (BrokenCircuitException)
                    {
                        _log.LogWarning(
                          "Circuit OPEN while indexing – '{File}' skipped", doc.FileName);
                        break;                  // don’t bother with the rest for now
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex,
                          "Failed embedding '{File}', continuing with others", doc.FileName);
                    }
                }

                _index = entries;
                _log.LogInformation(
                    "Index ready: {Docs} docs, elapsed {Ms} ms",
                    _index.Count, sw.ElapsedMilliseconds);
            }
            finally
            {
                _initLock.Release();
            }
        }



        private IEnumerable<(Document, string)> LoadDocumentsFromDisk()
        {
            foreach (var file in Directory.GetFiles(_dataFolder, "*.txt"))
            {
                yield return (new Document
                {
                    FileName = Path.GetFileName(file),
                    Text     = File.ReadAllText(file)
                }, File.ReadAllText(file));
            }
        }

        private List<Document> ComputeTopDocuments(float[] qVec, int topK)
        {
            var qNorm = ComputeNorm(qVec);
            return _index!
                .Select(item =>
                {
                    var dot = DotProduct(qVec, item.Vec);
                    var sim = dot / (qNorm * item.Norm);
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
            for (int i = 0; i < a.Length; i++) sum += a[i] * b[i];
            return sum;
        }

        private static float ComputeNorm(float[] v)
        {
            float sumSq = 0;
            foreach (var x in v) sumSq += x * x;
            return MathF.Sqrt(sumSq);
        }
    }
}

