using RagService.Domain.Models;

namespace RagService.Application.Interfaces;

public interface ILLMService
{
    Task<string> GenerateAsync(string query, IEnumerable<Document> contextDocs);
}
