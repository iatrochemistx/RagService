// src/RagService.Api/Program.cs
using RagService.Application.Interfaces;
using RagService.Infrastructure.Embeddings;
using RagService.Infrastructure.Llm;
using RagService.Infrastructure.VectorSearch;
using Microsoft.Extensions.Options;
using RagService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// bind your OpenAI options
builder.Services.Configure<OpenAiOptions>(
    builder.Configuration.GetSection("OpenAI"));

// register real OpenAI clients
builder.Services.AddHttpClient<IEmbeddingService, OpenAiEmbeddingService>();
builder.Services.AddHttpClient<ILLMService,     OpenAiLlmService>();

// always real vector search
builder.Services.AddSingleton<IVectorSearchService, VectorSearchService>();

// framework plumbing
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

// for WebApplicationFactory<Program>
public partial class Program { }
