using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RagService.Domain.Models;

namespace RagService.Application.Interfaces
{
    public interface IVectorSearchService
    {
        Task<List<Document>> GetTopDocumentsAsync(string query, int topK = 3, CancellationToken cancellationToken = default);
        Task<List<Document>> GetTopDocumentsAsync(float[] queryVector, int topK = 3, CancellationToken cancellationToken = default);
    }
}
