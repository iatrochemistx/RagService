// src/RagService.Api/Program.cs
using RagService.Application.Interfaces;
using RagService.Infrastructure;
using RagService.Infrastructure.Embeddings;
using RagService.Infrastructure.Llm;
using RagService.Infrastructure.VectorSearch;

var builder = WebApplication.CreateBuilder(args);

// Bind OpenAI options (BaseUrl, model names, etc.)
builder.Services.Configure<OpenAiOptions>(
    builder.Configuration.GetSection("OpenAI"));

// Decide runtime mode
bool useMocks = builder.Configuration.GetValue<bool>("UseMocks");

if (useMocks)
{
    builder.Services.AddSingleton<IEmbeddingService, MockEmbeddingService>();
    builder.Services.AddSingleton<ILLMService,     MockLlmService>();
}
else
{
    builder.Services.AddHttpClient<IEmbeddingService, OpenAiEmbeddingService>();
    builder.Services.AddHttpClient<ILLMService,     OpenAiLlmService>();
}

// Retrieval layer (shared by both modes)
builder.Services.AddSingleton<IVectorSearchService, VectorSearchService>();

// Standard ASP.NET Core setup
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

// For WebApplicationFactory in integration tests
public partial class Program { }
