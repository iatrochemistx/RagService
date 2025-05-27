// RetryPolicyTests.cs
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Xunit;

public class RetryPolicyTests
{
    [Fact]
    public async Task RetryPolicy_ShouldRetryOn429ThenSucceed()
    {
        // Arrange: a handler that returns 429 twice, then 200 OK
        int callCount = 0;
        var handler = new TestHandler(async request =>
        {
            callCount++;
            if (callCount < 3)
                return new HttpResponseMessage((HttpStatusCode)429);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        // Build HttpClient with our retry policy and test handler
        var services = new ServiceCollection();
        services.AddHttpClient("test")
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddPolicyHandler(GetRetryPolicy());

        var provider = services.BuildServiceProvider();
        var factory  = provider.GetRequiredService<IHttpClientFactory>();
        var client   = factory.CreateClient("test");

        // Act
        var response = await client.GetAsync("http://example.com");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, callCount); // 2 failures + 1 success
    }

    // The same retry policy you used in Program.cs
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == (HttpStatusCode)429)
            .WaitAndRetryAsync(new[]
            {
                TimeSpan.FromMilliseconds(10),
                TimeSpan.FromMilliseconds(10),
                TimeSpan.FromMilliseconds(10),
            });
    }

    // A simple DelegatingHandler that invokes a func to produce a response
    private class TestHandler : DelegatingHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handlerFunc;

        public TestHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handlerFunc)
        {
            _handlerFunc = handlerFunc;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => _handlerFunc(request);
    }
}
