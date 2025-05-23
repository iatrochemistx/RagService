using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RagService.Domain.Models;
using RagService.Tests.Utilities;

namespace RagService.Tests.Api
{
    public class QueryControllerIntegrationTests : IClassFixture<CustomWebAppFactory>
    {
        private readonly HttpClient _client;

        public QueryControllerIntegrationTests(CustomWebAppFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Query_returns_documents_without_llm_response()
        {
            var response = await _client.GetAsync("/query?q=test");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<RetrievalResult>();

            result.Should().NotBeNull();
            result!.Documents.Should().NotBeEmpty();
            result.Response.Should().BeNull();
        }

        [Fact]
        public async Task Query_with_llm_response_includes_answer()
        {
            var response = await _client.GetAsync("/query?q=test&response=true");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<RetrievalResult>();

            result.Should().NotBeNull();
            result!.Documents.Should().NotBeEmpty();
            result.Response.Should().NotBeNull();
            result.Response.Should().Contain("MOCK LLM ANSWER");
        }

        [Fact]
        public async Task Missing_query_returns_bad_request()
        {
            var response = await _client.GetAsync("/query");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
