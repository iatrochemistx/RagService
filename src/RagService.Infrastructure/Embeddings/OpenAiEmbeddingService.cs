// src/RagService.Infrastructure/Embeddings/OpenAiEmbeddingService.cs
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RagService.Application.Interfaces;
using RagService.Infrastructure;

namespace RagService.Infrastructure.Embeddings
{
    /// <summary>
    /// IEmbeddingService implementation calling OpenAI embeddings endpoint via HttpClient.
    /// </summary>
    public sealed class OpenAiEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenAiEmbeddingService> _logger;
        private readonly string _model;

        public OpenAiEmbeddingService(
            HttpClient httpClient,
            IOptions<OpenAiOptions> options,
            ILogger<OpenAiEmbeddingService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger     = logger     ?? throw new ArgumentNullException(nameof(logger));

            var opts = options?.Value ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(opts.ApiKey))
                throw new ArgumentException("OpenAI API key not configured.", nameof(options));

            _model = opts.EmbeddingModel;
            _httpClient.BaseAddress = new Uri(opts.BaseUrl);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", opts.ApiKey);
        }

        /// <inheritdoc />
        public async Task<float[]> EmbedAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text must not be empty.", nameof(text));

            _logger.LogInformation("Requesting embedding for text length {Length} characters.", text.Length);

            var payload = new { input = text, model = _model };

            using var response = await _httpClient.PostAsJsonAsync(
                                        "v1/embeddings", payload, cancellationToken)
                                    .ConfigureAwait(false);

            /* ---------- NEW: log JSON body on non-success for clear diagnostics ---------- */
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("OpenAI Embedding error {Status}: {Body}",
                                   (int)response.StatusCode, body);
            }
            /* --------------------------------------------------------------------------- */

            response.EnsureSuccessStatusCode();

            var embeddingResponse = await response.Content
                                                  .ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: cancellationToken)
                                                  .ConfigureAwait(false);

            var first = embeddingResponse?.Data?.FirstOrDefault();
            if (first?.Embedding is not { Length: > 0 } embedding)
                throw new InvalidOperationException("No embedding data returned from OpenAI.");

            _logger.LogInformation("Received embedding vector of length {Length}.", embedding.Length);
            return embedding;
        }

        private sealed record EmbeddingResponse(
            [property: JsonPropertyName("data")] EmbeddingData[] Data);

        private sealed record EmbeddingData(
            [property: JsonPropertyName("embedding")] float[] Embedding);
    }
}
