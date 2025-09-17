using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SKFProductAssistant.Functions.Functions;
using System.Net;
using System.Text.Json;
using Xunit;

namespace SKFProductAssistant.Functions.IntegrationTests
{
    public class RealDataIntegrationTests : IClassFixture<TestFixture>
    {
        private readonly TestFixture _fixture;

        public RealDataIntegrationTests(TestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task HealthCheck_ShouldReturnHealthy()
        {
            // Arrange
            var function = _fixture.GetService<ProductQueryFunction>();
            var httpRequest = _fixture.CreateHttpRequestData("GET", "/api/health");

            // Act
            var response = await function.HealthCheck(httpRequest);

            // Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseContent = await _fixture.ReadResponseContent(response);
            responseContent.Should().Contain("Healthy");
        }

        [Fact]
        public async Task StartConversation_ShouldReturnValidConversationId()
        {
            // Arrange
            var function = _fixture.GetService<ProductQueryFunction>();
            var httpRequest = _fixture.CreateHttpRequestData("POST", "/api/conversation/start");

            // Act
            var response = await function.StartConversation(httpRequest);

            // Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseContent = await _fixture.ReadResponseContent(response);
            responseContent.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task QueryProduct_ValidRequest_ShouldReturnResponse()
        {
            // Arrange
            var function = _fixture.GetService<ProductQueryFunction>();
            var request = new ProductQueryRequest
            {
                Query = "What is the width of 6205?"
            };

            var httpRequest = _fixture.CreateHttpRequestData("POST", "/api/query", request);

            // Act
            var response = await function.QueryProduct(httpRequest);

            // Assert
            response.Should().NotBeNull();

            var responseContent = await _fixture.ReadResponseContent(response);
            responseContent.Should().NotBeNullOrEmpty();

            // Response should be valid JSON
            var jsonDocument = JsonDocument.Parse(responseContent);
            jsonDocument.Should().NotBeNull();
        }

        [Theory]
        [InlineData("6205")]
        [InlineData("6025-N")]
        public async Task QueryProduct_DifferentProducts_ShouldReturnResponses(string productName)
        {
            // Arrange
            var function = _fixture.GetService<ProductQueryFunction>();
            var request = new ProductQueryRequest
            {
                Query = $"Tell me about {productName}"
            };

            var httpRequest = _fixture.CreateHttpRequestData("POST", "/api/query", request);

            // Act
            var response = await function.QueryProduct(httpRequest);

            // Assert
            response.Should().NotBeNull();

            var responseContent = await _fixture.ReadResponseContent(response);
            responseContent.Should().NotBeNullOrEmpty();
        }
    }

    public class ProductQueryRequest
    {
        public string Query { get; set; } = string.Empty;
        public string? ConversationId { get; set; }
    }
}
