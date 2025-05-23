using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RagService.Api;
using RagService.Application.Interfaces;
using RagService.Infrastructure.Embeddings;
using RagService.Infrastructure.Llm;

namespace RagService.Tests.Utilities
{
    /// <summary>
    /// Bootstraps the API for integration tests using in-memory mocks.
    /// </summary>
    public sealed class CustomWebAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var apiDir = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",
                "..",
                "src", "RagService.Api"));

            builder.UseContentRoot(apiDir);

            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(IEmbeddingService));
                services.RemoveAll(typeof(ILLMService));

                services.AddSingleton<IEmbeddingService, MockEmbeddingService>();
                services.AddSingleton<ILLMService, MockLlmService>();
            });
        }
    }
}
