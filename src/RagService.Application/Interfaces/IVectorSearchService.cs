using RagService.Domain.Models;

namespace RagService.Application.Interfaces;

public interface IVectorSearchService
{
    Task<List<Document>> GetTopDocumentsAsync(string query, int topK = 3);
    Task<List<Document>> GetTopDocumentsAsync(float[] queryVector, int topK = 3);
}
