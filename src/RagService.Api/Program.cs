// src/RagService.Api/Program.cs
using RagService.Application.Interfaces;
using RagService.Infrastructure.Embeddings;
using RagService.Infrastructure.Llm;
using RagService.Infrastructure.VectorSearch;
using Microsoft.Extensions.Options;
using RagService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------
// TEMPORARY: RUN 100 % IN MOCK MODE
// ---------------------------------------------------------------------

// You can leave this Configure call; it’s harmless in mock mode and
// makes it easy to switch back to real OpenAI later.
builder.Services.Configure<OpenAiOptions>(
    builder.Configuration.GetSection("OpenAI"));

// Register **mock** services (no HTTP calls, deterministic behaviour)
builder.Services.AddSingleton<IEmbeddingService, MockEmbeddingService>();
builder.Services.AddSingleton<ILLMService,     MockLlmService>();

// Vector search (in-memory); depends on IEmbeddingService above
builder.Services.AddSingleton<IVectorSearchService, VectorSearchService>();

// ---------------------------------------------------------------------
// Standard ASP-NET plumbing
// ---------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

// Needed by WebApplicationFactory<Program> in the test project
public partial class Program { }
