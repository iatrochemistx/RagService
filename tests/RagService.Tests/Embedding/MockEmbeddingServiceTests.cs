using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using RagService.Infrastructure.Embeddings;

namespace RagService.Tests.Embedding
{
    public class MockEmbeddingServiceTests
    {
        private readonly MockEmbeddingService _sut = new();

        [Fact]
        public async Task Same_input_returns_identical_vector()
        {
            var v1 = await _sut.EmbedAsync("hello world");
            var v2 = await _sut.EmbedAsync("hello world");
            v1.Should().Equal(v2, "embedding should be deterministic");
        }

        [Fact]
        public async Task Vector_dimension_is_384()
        {
            var vec = await _sut.EmbedAsync("anything");
            vec.Length.Should().Be(384);
        }

        [Fact]
        public async Task Each_component_is_in_minus1_to_plus1_range()
        {
            var vec = await _sut.EmbedAsync("range-check");
            vec.Should().OnlyContain(x => x >= -1 && x <= 1);
        }
    }
}
