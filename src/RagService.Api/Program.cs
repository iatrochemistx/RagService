// src/RagService.Api/Program.cs
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Polly;
using Polly.Extensions.Http;  
using RagService.Application.Interfaces;
using RagService.Infrastructure;
using RagService.Infrastructure.Embeddings;
using RagService.Infrastructure.Llm;
using RagService.Infrastructure.VectorSearch;


var builder = WebApplication.CreateBuilder(args);

// ---------- Resilience: retry with jitter ----------
static IEnumerable<TimeSpan> BackoffWithJitter()
{
    var rand = new Random();
    for (int i = 0; i < 3; i++)          // three retries
    {
        var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, i)); // 1s, 2s, 4s
        yield return baseDelay + TimeSpan.FromMilliseconds(rand.Next(0, 500));
    }
}

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()                                    // 5xx + network errors
    .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests) // 429
    .WaitAndRetryAsync(BackoffWithJitter());

// ---------- OpenAI options ----------
builder.Services.Configure<OpenAiOptions>(
    builder.Configuration.GetSection("OpenAI"));

// ---------- Decide mock vs real ----------
bool useMocks = builder.Configuration.GetValue<bool>("UseMocks");

if (useMocks)
{
    builder.Services.AddSingleton<IEmbeddingService, MockEmbeddingService>();
    builder.Services.AddSingleton<ILLMService,     MockLlmService>();
}
else
{
    // Real OpenAI HTTP clients with retry & timeout
    builder.Services.AddHttpClient<IEmbeddingService, OpenAiEmbeddingService>()
                    .AddPolicyHandler(retryPolicy)
                    .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(30));

    builder.Services.AddHttpClient<ILLMService, OpenAiLlmService>()
                    .AddPolicyHandler(retryPolicy)
                    .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(30));
}

// ---------- Shared vector search ----------
builder.Services.AddSingleton<IVectorSearchService, VectorSearchService>();

//Rate limiting
var rateCfg       = builder.Configuration.GetSection("RateLimiting");
int permitLimit   = rateCfg.GetValue<int>("PermitLimit");     // e.g. 60
int windowSeconds = rateCfg.GetValue<int>("WindowSeconds");   // e.g. 60
int queueLimit    = rateCfg.GetValue<int>("QueueLimit");      // e.g. 0

builder.Services.AddRateLimiter(opts =>
{
    // Return 429 instead of 503 when throttled
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Optional: custom body / Retry-After header
    opts.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        // add “Retry-After” = window length
        ctx.HttpContext.Response.Headers["Retry-After"] = windowSeconds.ToString();
        await ctx.HttpContext.Response.WriteAsync(
            "Rate limit exceeded. Please try again later.", token);
    };

    // GLOBAL limiter — one bucket per client IP address
    opts.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpCtx =>
    {
        var ip = httpCtx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey : ip,                 // each IP gets its own counter
            factory      : _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = permitLimit,
                Window               = TimeSpan.FromSeconds(windowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = queueLimit,
                AutoReplenishment    = true
            });
    });
});

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
app.UseRateLimiter();
app.MapControllers();
app.Run();

// Needed by WebApplicationFactory in tests
public partial class Program { }
