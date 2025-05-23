// src/RagService.Api/Program.cs
using RagService.Application.Interfaces;
using RagService.Infrastructure.Embeddings;
using RagService.Infrastructure.Llm;
using RagService.Infrastructure.VectorSearch;

var builder = WebApplication.CreateBuilder(args);

<<<<<<< HEAD
// ─── Register your RAG infrastructure services ─────────────────────────────────
builder.Services.AddSingleton<IEmbeddingService,    MockEmbeddingService>();
builder.Services.AddSingleton<ILLMService,          MockLlmService>();
builder.Services.AddSingleton<IVectorSearchService, VectorSearchService>();
// ────────────────────────────────────────────────────────────────────────────────
=======
// Application services
builder.Services.AddSingleton<RagService.Application.Interfaces.IEmbeddingService, RagService.Infrastructure.Embeddings.MockEmbeddingService>();
builder.Services.AddSingleton<RagService.Application.Interfaces.ILLMService, RagService.Infrastructure.Llm.MockLlmService>();
builder.Services.AddSingleton<RagService.Application.Interfaces.IVectorSearchService, RagService.Infrastructure.VectorSearch.VectorSearchService>();

// Add services to the container.
>>>>>>> 0fcfa3a5572acc68add741a2f800e7ec6462800a

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
