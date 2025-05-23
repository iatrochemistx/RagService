// src/RagService.Api/Program.cs
using RagService.Application.Interfaces;
using RagService.Infrastructure.Embeddings;
using RagService.Infrastructure.Llm;
using RagService.Infrastructure.VectorSearch;

var builder = WebApplication.CreateBuilder(args);

// ─── Register your RAG infrastructure services ─────────────────────────────────
builder.Services.AddSingleton<IEmbeddingService,    MockEmbeddingService>();
builder.Services.AddSingleton<ILLMService,          MockLlmService>();
builder.Services.AddSingleton<IVectorSearchService, VectorSearchService>();
// ────────────────────────────────────────────────────────────────────────────────

// Add framework services
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

// Expose the Program class for WebApplicationFactory<Program>
public partial class Program { }
