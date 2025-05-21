using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using RagService.Infrastructure.Llm;
using RagService.Domain.Models;

namespace RagService.Tests.Llm
{
    public class MockLlmServiceTests
    {
        private readonly MockLlmService _sut = new();

        private static IEnumerable<Document> MakeDocs(params string[] names)
        {
            int id = 0;
            foreach (var n in names)
            {
                yield return new Document { Id = id++, FileName = n };
            }
        }

        [Fact]
        public async Task Answer_echoes_the_query()
        {
            // Arrange
            const string query = "What is a fox?";

            // Act
            var answer = await _sut.GenerateAsync(query, MakeDocs("alpha.txt"));

            // Assert
            answer.Should().Contain(query);
        }

        [Fact]
        public async Task Answer_lists_all_document_filenames()
        {
            var docs = new[] { "alpha.txt", "beta.txt", "gamma.txt" };
            var answer = await _sut.GenerateAsync("question", MakeDocs(docs));

            foreach (var name in docs)
                answer.Should().Contain(name);
        }

        [Fact]
        public async Task Answer_is_deterministic_for_same_inputs()
        {
            var docs = MakeDocs("alpha.txt");
            var a1   = await _sut.GenerateAsync("same query", docs);
            var a2   = await _sut.GenerateAsync("same query", docs);

            a1.Should().Be(a2);
        }
    }
}