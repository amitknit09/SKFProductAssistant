// Application/Services/ProductQueryService.cs
using Microsoft.Extensions.Logging;
using SKFProductAssistant.Application.DTOs;
using SKFProductAssistant.Application.Interfaces;
using SKFProductAssistant.Domain.Entities;
using SKFProductAssistant.Domain.Enums;
using SKFProductAssistant.Domain.ValueObjects;

namespace SKFProductAssistant.Application.Services
{
    public class ProductQueryService : IProductQueryService
    {
        private readonly IAIService _aiService;
        private readonly IProductService _productService;
        private readonly ILogger<ProductQueryService> _logger;

        public ProductQueryService(
            IAIService aiService,
            IProductService productService,
            ILogger<ProductQueryService> logger)
        {
            _aiService = aiService;
            _productService = productService;
            _logger = logger;
        }

        public async Task<ProductQueryResponseDto> ProcessQueryAsync(string query, Conversation conversation)
        {
            try
            {
                // Extract product name and attribute using AI - FIXED: Separate async calls
                var productNameTask = _aiService.ExtractProductNameAsync(query, conversation);
                var attributeTask = _aiService.ExtractAttributeAsync(query, conversation);

                // Await both tasks
                var productName = await productNameTask;
                var attribute = await attributeTask;

                return await ProcessExtractedInformation(productName, attribute, query, conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing query: {Query}", query);
                return new ProductQueryResponseDto
                {
                    Success = false,
                    Message = "An error occurred while processing your query",
                    ResultType = QueryResultType.SystemError
                };
            }
        }

        private async Task<ProductQueryResponseDto> ProcessExtractedInformation(
            ProductName? productName,
            string? attribute,
            string originalQuery,
            Conversation conversation)
        {
            // Validate product name
            if (productName == null)
            {
                return await HandleMissingProduct(originalQuery, conversation);
            }

            // Validate product exists
            var productExists = await _productService.ProductExistsAsync(productName);
            if (!productExists)
            {
                return await HandleInvalidProduct(productName, originalQuery, conversation);
            }

            // Validate attribute
            if (string.IsNullOrEmpty(attribute))
            {
                return await HandleMissingAttribute(productName, originalQuery, conversation);
            }

            // Get product information
            var product = await _productService.GetProductByNameAsync(productName);
            if (product == null)
            {
                return new ProductQueryResponseDto
                {
                    Success = false,
                    Message = "Product not found",
                    ResultType = QueryResultType.ProductNotFound
                };
            }

            var productAttribute = product.GetAttribute(attribute);
            if (productAttribute == null)
            {
                return await HandleMissingAttributeValue(productName, attribute, conversation, product);
            }

            // Update conversation context
            conversation.SetLastProductDiscussed(productName);

            // Generate response
            var answer = await _aiService.GenerateResponseAsync(
                productName,
                productAttribute.Name,
                productAttribute.Value,
                productAttribute.Unit,
                conversation
            );

            return new ProductQueryResponseDto
            {
                Success = true,
                Answer = answer,
                Message = "Information retrieved successfully",
                ProductDetails = new ProductDetailsDto
                {
                    ProductName = product.Name,
                    Attribute = productAttribute.Name,
                    Value = productAttribute.Value,
                    Unit = productAttribute.Unit,
                    AllAttributes = product.Attributes.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.GetFormattedValue()
                    )
                },
                ResultType = QueryResultType.Success
            };
        }

        private async Task<ProductQueryResponseDto> HandleMissingProduct(string query, Conversation conversation)
        {
            var response = await _aiService.GenerateConversationalResponseAsync(query, conversation);

            return new ProductQueryResponseDto
            {
                Success = false,
                Message = "Product not specified",
                Answer = response,
                Suggestions = new List<string> { "Please specify an SKF product name (e.g., 6205, 6206-2RS1)" },
                ResultType = QueryResultType.InvalidQuery
            };
        }

        private async Task<ProductQueryResponseDto> HandleInvalidProduct(ProductName productName, string query, Conversation conversation)
        {
            var similarProducts = await _productService.FindSimilarProductsAsync(productName);

            var answer = $"I couldn't find product '{productName}' in our database.";
            if (similarProducts.Any())
            {
                answer += $" Did you mean: {string.Join(", ", similarProducts)}?";
            }

            return new ProductQueryResponseDto
            {
                Success = false,
                Message = "Product not found",
                Answer = answer,
                Suggestions = similarProducts.Select(p => p.Value).ToList(),
                ResultType = QueryResultType.ProductNotFound
            };
        }

        private async Task<ProductQueryResponseDto> HandleMissingAttribute(ProductName productName, string query, Conversation conversation)
        {
            var product = await _productService.GetProductByNameAsync(productName);
            var availableAttributes = product?.GetAvailableAttributes() ?? new List<string>();

            var answer = $"What would you like to know about the {productName}? Available information: {string.Join(", ", availableAttributes.Take(5))}";

            return new ProductQueryResponseDto
            {
                Success = false,
                Message = "Attribute not specified",
                Answer = answer,
                Suggestions = availableAttributes.Take(5).ToList(),
                ResultType = QueryResultType.InvalidQuery
            };
        }

        private async Task<ProductQueryResponseDto> HandleMissingAttributeValue(
            ProductName productName,
            string attribute,
            Conversation conversation,
            Product product)
        {
            var availableAttributes = product.GetAvailableAttributes().Take(5).ToList();

            var answer = $"I don't have {attribute} information for {productName}. Available data: {string.Join(", ", availableAttributes)}";

            return new ProductQueryResponseDto
            {
                Success = false,
                Message = "Attribute data not available",
                Answer = answer,
                Suggestions = availableAttributes,
                ResultType = QueryResultType.AttributeNotFound
            };
        }
    }
}
