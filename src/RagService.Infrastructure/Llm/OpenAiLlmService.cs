// src/RagService.Infrastructure/Llm/OpenAiLlmService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RagService.Application.Interfaces;
using RagService.Domain.Models;
using RagService.Infrastructure;

namespace RagService.Infrastructure.Llm
{
    /// <summary>
    /// ILLMService implementation calling OpenAI's Chat Completions API via HttpClient.
    /// </summary>
    public sealed class OpenAiLlmService : ILLMService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenAiLlmService> _logger;
        private readonly string _model;

        public OpenAiLlmService(
            HttpClient httpClient,
            IOptions<OpenAiOptions> options,
            ILogger<OpenAiLlmService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger     = logger     ?? throw new ArgumentNullException(nameof(logger));

            var opts = options?.Value ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(opts.ApiKey))
                throw new ArgumentException("OpenAI API key not configured.", nameof(options));

            _model = opts.ChatModel;
            _httpClient.BaseAddress = new Uri(opts.BaseUrl);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", opts.ApiKey);
        }

        /// <inheritdoc />
        public async Task<string> GenerateAsync(
            string query,
            IEnumerable<Document> contextDocs,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query must not be empty.", nameof(query));

            // Build system prompt from context documents
            var systemPrompt = string.Join("\n", contextDocs.Select(d => d.Text));
            _logger.LogInformation("Sending chat request to OpenAI with {DocCount} documents.", contextDocs.Count());

            // Prepare request payload
            var messages = new[] {
                new ChatMessage("system", systemPrompt),
                new ChatMessage("user",   query)
            };
            var payload = new ChatRequest(_model, messages);

            // Send request
            using var response = await _httpClient.PostAsJsonAsync("v1/chat/completions", payload, cancellationToken)
                                                  .ConfigureAwait(false);

            /* ---------- NEW: log JSON body on non-success for clear diagnostics ---------- */
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("OpenAI Chat error {Status}: {Body}",
                                   (int)response.StatusCode, body);
            }
            /* --------------------------------------------------------------------------- */

            response.EnsureSuccessStatusCode();

            var chatResponse = await response.Content
                                             .ReadFromJsonAsync<ChatResponse>(cancellationToken: cancellationToken)
                                             .ConfigureAwait(false);

            var message = chatResponse?.Choices?.FirstOrDefault()?.Message;
            if (message == null || string.IsNullOrWhiteSpace(message.Content))
                throw new InvalidOperationException("OpenAI returned no chat response.");

            _logger.LogInformation("Received chat response: {Snippet}",
                                   message.Content[..Math.Min(50, message.Content.Length)]);
            return message.Content.Trim();
        }

        /* ---------- DTOs ---------- */

        private sealed record ChatMessage(
            [property: JsonPropertyName("role")]    string Role,
            [property: JsonPropertyName("content")] string Content);

        private sealed record ChatRequest(
            [property: JsonPropertyName("model")]    string Model,
            [property: JsonPropertyName("messages")] IEnumerable<ChatMessage> Messages);

        private sealed record Choice(
            [property: JsonPropertyName("message")] ChatMessage Message);

        private sealed record ChatResponse(
            [property: JsonPropertyName("choices")] Choice[] Choices);
    }
}
