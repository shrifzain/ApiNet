using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ProNet.Api.Tests
{
    public class HelloEndpointTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public HelloEndpointTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task HelloEndpoint_ReturnsExpectedMessage()
        {
            // Act
            var response = await _client.GetAsync("/api/hello");

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            var result = await response.Content.ReadFromJsonAsync<HelloResponse>();

            Assert.Equal("Hello World from .NEkkppT 6! cae", result?.Message);
        }

        private class HelloResponse
        {
            public string? Message { get; set; }
        }
    }
}
