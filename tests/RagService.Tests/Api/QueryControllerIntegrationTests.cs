// File: tests/RagService.Tests/Utilities/CustomWebAppFactory.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.IO;
using RagService.Api;                    // Program
using RagService.Application.Interfaces; // IEmbeddingService, ILlmService
using RagService.Tests.Embedding;        // MockEmbeddingService
using RagService.Tests.Llm;
using RagService.Infrastructure.Embeddings;
using RagService.Infrastructure.Llm;              // MockLlmService

namespace RagService.Tests.Utilities
{
    /// <summary>
    /// Bootstraps the API for integration tests, but replaces any
    /// “real” network-bound services with in-process test doubles.
    /// </summary>
    public sealed class CustomWebAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // 1️⃣  Make sure the host can find Controllers + data\*.txt
            //     (content root must point at the API project folder).
            var apiDir = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,          // …\tests\bin\Debug\net7.0
                "..", "..", "..", "..",            // up to …\tests
                "..",                              // up to repo root
                "src", "RagService.Api"));         // API project folder
            builder.UseContentRoot(apiDir);

            // 2️⃣  Swap out the real OpenAI services for mocks.
            builder.ConfigureServices(services =>
            {
                // Remove anything the production code registered.
                services.RemoveAll(typeof(IEmbeddingService));
                services.RemoveAll(typeof(ILLMService));

                // Register lightweight, deterministic test doubles.
                services.AddSingleton<IEmbeddingService, MockEmbeddingService>();
                services.AddSingleton<ILLMService, MockLlmService>();
            });
        }
    }
}
