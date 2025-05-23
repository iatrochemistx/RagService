var builder = WebApplication.CreateBuilder(args);

// Application services
builder.Services.AddSingleton<RagService.Application.Interfaces.IEmbeddingService, RagService.Infrastructure.Embeddings.MockEmbeddingService>();
builder.Services.AddSingleton<RagService.Application.Interfaces.ILLMService, RagService.Infrastructure.Llm.MockLlmService>();
builder.Services.AddSingleton<RagService.Application.Interfaces.IVectorSearchService, RagService.Infrastructure.VectorSearch.VectorSearchService>();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();  

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
public partial class Program { }
