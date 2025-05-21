using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using RagService.Infrastructure.VectorSearch;
using RagService.Infrastructure.Embeddings;

namespace RagService.Tests.VectorSearch
{
    /// <summary>
    /// Verifies VectorSearchService fulfils the spec requirements:
    ///  – loads /data on startup
    ///  – returns top-K documents ranked by cosine similarity
    ///  – second overload (pre-computed vector) is equivalent
    /// </summary>
    public sealed class VectorSearchServiceTests : IDisposable
    {
        private readonly string _dataDir;
        private readonly VectorSearchService _sut;

        public VectorSearchServiceTests()
        {
            // Arrange test-local /data folder under the test assembly’s bin dir
            _dataDir = Path.Combine(AppContext.BaseDirectory, "data");
            Directory.CreateDirectory(_dataDir);

            File.WriteAllText(Path.Combine(_dataDir, "alpha.txt"),
                "The quick brown fox jumps over the lazy dog.");
            File.WriteAllText(Path.Combine(_dataDir, "beta.txt"),
                "A slow red turtle crawls under the sleepy cat.");

            // Use deterministic mock embedder for reproducible scores
            _sut = new VectorSearchService(new MockEmbeddingService());
        }

        [Fact]
        public async Task Query_returns_at_least_one_document()
        {
            var results = await _sut.GetTopDocumentsAsync("quick");
            results.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Returned_documents_are_ordered_by_similarity()
        {
            var results = await _sut.GetTopDocumentsAsync("quick", topK: 2);

            // alpha.txt contains the word "quick" -> should rank higher than beta.txt
            results.First().FileName.Should().Be("alpha.txt");
            results.Last().FileName.Should().Be("beta.txt");
        }

        [Fact]
        public async Task Vector_overload_returns_same_first_document()
        {
            var query     = "quick";
            var queryVec  = await new MockEmbeddingService().EmbedAsync(query);

            var byText    = await _sut.GetTopDocumentsAsync(query,    topK: 1);
            var byVector  = await _sut.GetTopDocumentsAsync(queryVec, topK: 1);

            byVector.Single().FileName.Should().Be(byText.Single().FileName);
        }

        /* ------------- tear-down ------------- */
        public void Dispose() => Directory.Delete(_dataDir, recursive: true);
    }
}
