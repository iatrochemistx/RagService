using System.Text;
using System.Threading.Tasks;
using RagService.Application.Interfaces;
using RagService.Domain.Models;

namespace RagService.Infrastructure.Llm
{
    /// <summary>
    /// A deterministic “mock” LLM service for local development and testing.
    /// It echoes the query and lists the context document names.
    /// </summary>
    public sealed class MockLlmService : ILLMService
    {
        public Task<string> GenerateAsync(string query, IEnumerable<Document> contextDocs)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[MOCK LLM ANSWER] You asked: \"{query}\".");
            sb.Append("Context docs: ");
            sb.Append(string.Join(", ", contextDocs.Select(d => d.FileName)));
            sb.Append(".");
            return Task.FromResult(sb.ToString());
        }
    }
}
