using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RagService.Domain.Models;

namespace RagService.Application.Interfaces
{
    public interface ILLMService
    {
        Task<string> GenerateAsync(string query, IEnumerable<Document> contextDocs, CancellationToken cancellationToken = default);
    }
}
