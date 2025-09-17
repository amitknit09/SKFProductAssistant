// Functions/ProductQueryFunction.cs
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SKFProductAssistant.Application.Commands;
using SKFProductAssistant.Application.Queries;
using System.Net;
using System.Text.Json;

namespace SKFProductAssistant.Functions.Functions
{
    public class ProductQueryFunction
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ProductQueryFunction> _logger;

        public ProductQueryFunction(IMediator mediator, ILogger<ProductQueryFunction> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [Function("StartConversation")]
        public async Task<HttpResponseData> StartConversation(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "conversation/start")] HttpRequestData req)
        {
            _logger.LogInformation("Starting new conversation");

            try
            {
                var command = new StartConversationCommand();
                var result = await _mediator.Send(command);

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                await response.WriteStringAsync(JsonSerializer.Serialize(result));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting conversation");
                return await CreateErrorResponse(req, "Failed to start conversation", HttpStatusCode.InternalServerError);
            }
        }

        [Function("QueryProduct")]
        public async Task<HttpResponseData> QueryProduct(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "query")] HttpRequestData req)
        {
            _logger.LogInformation("Processing product query");

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var queryRequest = JsonSerializer.Deserialize<ProductQueryRequest>(requestBody);

                if (queryRequest == null || string.IsNullOrWhiteSpace(queryRequest.Query))
                {
                    return await CreateErrorResponse(req, "Invalid query format", HttpStatusCode.BadRequest);
                }

                var query = new ProcessProductQueryQuery(queryRequest.Query, queryRequest.ConversationId);
                var result = await _mediator.Send(query);

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                await response.WriteStringAsync(JsonSerializer.Serialize(result));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing query");
                return await CreateErrorResponse(req, "Internal server error", HttpStatusCode.InternalServerError);
            }
        }

        [Function("GetProductAttribute")]
        public async Task<HttpResponseData> GetProductAttribute(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "product/{productName}/attribute/{attributeName}")]
            HttpRequestData req,
            string productName,
            string attributeName)
        {
            _logger.LogInformation("Getting product attribute: {ProductName}.{AttributeName}", productName, attributeName);

            try
            {
                var query = new GetProductAttributeQuery(productName, attributeName);
                var result = await _mediator.Send(query);

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                await response.WriteStringAsync(JsonSerializer.Serialize(result));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product attribute");
                return await CreateErrorResponse(req, "Internal server error", HttpStatusCode.InternalServerError);
            }
        }

        [Function("HealthCheck")]
        public async Task<HttpResponseData> HealthCheck(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            var healthStatus = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0"
            };

            await response.WriteStringAsync(JsonSerializer.Serialize(healthStatus));
            return response;
        }

        private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, string message, HttpStatusCode statusCode)
        {
            var response = req.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "application/json");

            var errorResponse = new
            {
                Success = false,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            await response.WriteStringAsync(JsonSerializer.Serialize(errorResponse));
            return response;
        }
    }

    public class ProductQueryRequest
    {
        public string Query { get; set; } = string.Empty;
        public string? ConversationId { get; set; }
    }
}
