// tests/RagService.Tests/Integration/OpenAiServiceIntegrationTests.cs
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RagService.Application.Interfaces;
using RagService.Domain.Models;
using RagService.Infrastructure;
using RagService.Infrastructure.Embeddings;
using RagService.Infrastructure.Llm;
using Xunit;

namespace RagService.Tests.Integration
{
    /// <summary>
    /// Live integration tests against the real OpenAI API.
    /// These tests require a valid OPENAI_API_KEY and skip on rate limits (429).
    /// </summary>
    public class OpenAiServiceIntegrationTests
    {
        private readonly IEmbeddingService _embeddings;
        private readonly ILLMService _llm;

        public OpenAiServiceIntegrationTests()
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                return;

            var services = new ServiceCollection();
            services.AddOptions<OpenAiOptions>()
                .Configure(opts =>
                {
                    opts.ApiKey         = apiKey;
                    opts.BaseUrl        = "https://api.openai.com/";
                    opts.EmbeddingModel = "text-embedding-ada-002";
                    opts.ChatModel      = "gpt-3.5-turbo";
                });

            services.AddLogging(lb => lb.AddConsole());
            services.AddHttpClient<IEmbeddingService, OpenAiEmbeddingService>();
            services.AddHttpClient<ILLMService,     OpenAiLlmService>();

            var provider = services.BuildServiceProvider();
            _embeddings = provider.GetService<IEmbeddingService>();
            _llm        = provider.GetService<ILLMService>();
        }

        [Fact(DisplayName = "EmbeddingService returns non-empty vector")]
        public async Task EmbeddingService_Returns_NonEmptyVector()
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.WriteLine("[SKIPPED] OPENAI_API_KEY not set.");
                return;
            }

            try
            {
                var vector = await _embeddings.EmbedAsync("Hello world", CancellationToken.None);
                vector.Should().NotBeNull().And.NotBeEmpty();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                Console.WriteLine("[SKIPPED] Rate limited (HTTP 429).");
                return;
            }
        }

        [Fact(DisplayName = "LLMService returns meaningful answer")]
        public async Task LlmService_Returns_MeaningfulAnswer()
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.WriteLine("[SKIPPED] OPENAI_API_KEY not set.");
                return;
            }

            var docs = new[] { new Document { FileName = "dummy.txt", Text = "OpenAI provides AI APIs." } };
            try
            {
                var answer = await _llm.GenerateAsync("What does OpenAI provide?", docs, CancellationToken.None);
                answer.Should().NotBeNullOrWhiteSpace()
                      .And.Contain("API", "because the answer should mention API");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                Console.WriteLine("[SKIPPED] Rate limited (HTTP 429).");
                return;
            }
        }
    }
}




