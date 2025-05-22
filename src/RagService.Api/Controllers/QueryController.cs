using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RagService.Application.Interfaces;
using RagService.Domain.Models;

namespace RagService.Api.Controllers
{
    /// <summary>
    /// Handles retrieval-augmented generation queries.
    /// </summary>
    [ApiController]
    [Route("query")]
    [Produces("application/json")]
    public class QueryController : ControllerBase
    {
        private readonly IVectorSearchService _search;
        private readonly ILLMService           _llm;
        private readonly ILogger<QueryController> _log;

        public QueryController(
            IVectorSearchService search,
            ILLMService llm,
            ILogger<QueryController> log)
        {
            _search = search;
            _llm    = llm;
            _log    = log;
        }

        /// <summary>
        /// Retrieves the top-K documents for a given query, optionally augmented by an LLM response.
        /// </summary>
        /// <param name="q">The user’s text query.</param>
        /// <param name="response">Whether to include an LLM-generated answer.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>List of matching document filenames and optional LLM answer.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(RetrievalResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails),  StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<RetrievalResult>> GetAsync(
            [FromQuery(Name = "q")] string q,
            [FromQuery] bool response = false,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                var pd = new ProblemDetails
                {
                    Title = "Missing required parameter",
                    Detail = "The 'q' parameter (query) must be provided and non-empty.",
                    Status = StatusCodes.Status400BadRequest
                };
                return BadRequest(pd);
            }

            _log.LogInformation("Received query: {Query}", q);

            // 1) Retrieve top documents
            var docs = await _search.GetTopDocumentsAsync(q,cancellationToken: ct);

            // 2) Optionally generate LLM response
            string? answer = null;
            if (response)
            {
                answer = await _llm.GenerateAsync(q, docs, ct);
            }

            var result = new RetrievalResult
            {
                Documents = docs.Select(d => d.FileName).ToList(),
                Response  = answer
            };

            _log.LogInformation("Query '{Query}' returned {Count} documents", q, result.Documents.Count);

            return Ok(result);
        }
    }
}
