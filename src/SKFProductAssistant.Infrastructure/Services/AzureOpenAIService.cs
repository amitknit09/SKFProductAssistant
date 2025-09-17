using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SKFProductAssistant.Application.DTOs;
using SKFProductAssistant.Application.Interfaces;
using SKFProductAssistant.Domain.Entities;
using SKFProductAssistant.Domain.ValueObjects;
using System.Text;

namespace SKFProductAssistant.Infrastructure.Services
{
    public class AzureOpenAIService : IAIService
    {
        private readonly OpenAIClient _client;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureOpenAIService> _logger;
        private readonly string _deploymentName;
        private readonly string _modelName;
        private readonly string _apiVersion;
        private readonly int _maxTokens;
        private readonly float _temperature;
        private readonly string _systemPrompt;

        public AzureOpenAIService(IConfiguration configuration, ILogger<AzureOpenAIService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var endpoint = _configuration["AzureOpenAI:Endpoint"]
                ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured");
            var apiKey = _configuration["AzureOpenAI:ApiKey"]
                ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is not configured");

            _deploymentName = _configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o-mini";
            _modelName = _configuration["AzureOpenAI:ModelName"] ?? "gpt-4o-mini";
            _apiVersion = _configuration["AzureOpenAI:ApiVersion"] ?? "2024-08-01-preview";
            _maxTokens = int.Parse(_configuration["AzureOpenAI:MaxTokens"] ?? "4000");
            _temperature = float.Parse(_configuration["AzureOpenAI:Temperature"] ?? "0.7");
            _systemPrompt = _configuration["AzureOpenAI:SystemPrompt"] ??
                "You are an expert SKF bearing assistant. Provide accurate, technical information about SKF bearings based on the provided specifications. Be concise and professional.";

            var clientOptions = new OpenAIClientOptions()
            {
                Retry = { NetworkTimeout = TimeSpan.FromSeconds(30) }
            };

            _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey), clientOptions);

            _logger.LogInformation("Azure OpenAI Service initialized with model: {ModelName}, deployment: {DeploymentName}, API version: {ApiVersion}",
                _modelName, _deploymentName, _apiVersion);
        }

        public async Task<ProductName?> ExtractProductNameAsync(string query, Conversation conversation)
        {
            try
            {
                var prompt = BuildProductExtractionPrompt(query, conversation);
                var response = await CallOpenAIAsync(prompt, maxTokens: 100);

                if (string.IsNullOrWhiteSpace(response))
                    return null;

                var productName = response.Trim().Replace("\"", "").Replace("'", "");

                if (string.IsNullOrWhiteSpace(productName) || productName.ToLower() == "none" || productName.ToLower() == "unknown")
                    return null;

                return new ProductName(productName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting product name from query: {Query}", query);
                return null;
            }
        }

        public async Task<string?> ExtractAttributeAsync(string query, Conversation conversation)
        {
            try
            {
                var prompt = BuildAttributeExtractionPrompt(query, conversation);
                var response = await CallOpenAIAsync(prompt, maxTokens: 100);

                if (string.IsNullOrWhiteSpace(response))
                    return null;

                var attribute = response.Trim().Replace("\"", "").Replace("'", "").ToLowerInvariant();

                if (attribute == "none" || attribute == "unknown")
                    return null;

                return attribute;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting attribute from query: {Query}", query);
                return null;
            }
        }

        public async Task<string> GenerateResponseAsync(ProductName productName, string attributeName,
            string attributeValue, string? unit, Conversation conversation)
        {
            try
            {
                var prompt = BuildResponseGenerationPrompt(productName, attributeName, attributeValue, unit, conversation);
                var response = await CallOpenAIAsync(prompt);

                return response ?? $"The {attributeName} of the SKF {productName} bearing is {attributeValue}{unit}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating response for product: {ProductName}, attribute: {AttributeName}",
                    productName, attributeName);
                return $"The {attributeName} of the SKF {productName} bearing is {attributeValue}{unit}.";
            }
        }

        // FIXED: Updated method signature to include ProductDetailsDto parameter
        public async Task<string> GenerateConversationalResponseAsync(string query, Conversation conversation, ProductDetailsDto? productDetails)
        {
            try
            {
                var prompt = BuildConversationalPrompt(query, conversation, productDetails);
                var response = await CallOpenAIAsync(prompt);

                return response ?? "I'm here to help you with SKF bearing information. Please ask me about specific bearings and their properties.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating conversational response for query: {Query}", query);
                return "I'm here to help you with SKF bearing information. Please ask me about specific bearings and their properties.";
            }
        }

        public async Task<ProductName?> ValidateProductExistenceAsync(ProductName productName, List<ProductName> availableProducts)
        {
            try
            {
                var exactMatch = availableProducts.FirstOrDefault(p =>
                    string.Equals(p.Value, productName.Value, StringComparison.OrdinalIgnoreCase));

                if (exactMatch != null)
                {
                    _logger.LogDebug("Exact match found for product: {ProductName}", productName);
                    return exactMatch;
                }

                var prompt = BuildProductValidationPrompt(productName, availableProducts);
                var response = await CallOpenAIAsync(prompt, maxTokens: 200);

                if (string.IsNullOrWhiteSpace(response))
                    return null;

                var suggestedProduct = response.Trim().Replace("\"", "").Replace("'", "");

                if (suggestedProduct.ToLower() == "none" || suggestedProduct.ToLower() == "no match")
                    return null;

                var validMatch = availableProducts.FirstOrDefault(p =>
                    string.Equals(p.Value, suggestedProduct, StringComparison.OrdinalIgnoreCase));

                if (validMatch != null)
                {
                    _logger.LogDebug("AI suggested valid product match: {OriginalProduct} -> {MatchedProduct}",
                        productName, validMatch);
                    return validMatch;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating product existence: {ProductName}", productName);
                return null;
            }
        }

        private async Task<string?> CallOpenAIAsync(string prompt, int? maxTokens = null)
        {
            try
            {
                var chatCompletionsOptions = new ChatCompletionsOptions()
                {
                    DeploymentName = _deploymentName,
                    Messages = {
                        new ChatRequestSystemMessage(_systemPrompt),
                        new ChatRequestUserMessage(prompt)
                    },
                    MaxTokens = maxTokens ?? _maxTokens,
                    Temperature = _temperature,
                    FrequencyPenalty = 0,
                    PresencePenalty = 0
                };

                var response = await _client.GetChatCompletionsAsync(chatCompletionsOptions);

                if (response?.Value?.Choices?.Count > 0)
                {
                    return response.Value.Choices[0].Message.Content;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Azure OpenAI");
                throw;
            }
        }

        private string BuildProductExtractionPrompt(string query, Conversation conversation)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("Extract the SKF bearing product name from the user's query.");
            prompt.AppendLine("Return only the product name (e.g., '6205', '6206-2RS1', '6025-N') or 'none' if no product is mentioned.");

            if (conversation.LastProductDiscussed != null)
            {
                prompt.AppendLine($"Context: Previously discussed product was '{conversation.LastProductDiscussed}'.");
            }

            prompt.AppendLine($"Query: {query}");
            prompt.AppendLine("Product name:");

            return prompt.ToString();
        }

        private string BuildAttributeExtractionPrompt(string query, Conversation conversation)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("Extract the bearing attribute being asked about from the user's query.");
            prompt.AppendLine("Common attributes include: width, inner_diameter, outer_diameter, dynamic_load_rating, static_load_rating, limiting_speed, mass");
            prompt.AppendLine("Return the attribute name in lowercase with underscores or 'none' if unclear.");
            prompt.AppendLine($"Query: {query}");
            prompt.AppendLine("Attribute:");

            return prompt.ToString();
        }

        private string BuildResponseGenerationPrompt(ProductName productName, string attributeName,
            string attributeValue, string? unit, Conversation conversation)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("Generate a professional response about the SKF bearing attribute.");
            prompt.AppendLine($"Product: SKF {productName}");
            prompt.AppendLine($"Attribute: {attributeName}");
            prompt.AppendLine($"Value: {attributeValue}{unit}");
            prompt.AppendLine("Response:");

            return prompt.ToString();
        }

        // FIXED: Updated to include ProductDetailsDto parameter
        private string BuildConversationalPrompt(string query, Conversation conversation, ProductDetailsDto? productDetails)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("Respond to this SKF bearing related query in a helpful, professional manner.");
            prompt.AppendLine("Keep responses concise and focused on SKF bearings.");

            if (conversation.LastProductDiscussed != null)
            {
                prompt.AppendLine($"Context: Previously discussed product was '{conversation.LastProductDiscussed}'.");
            }

            if (productDetails != null)
            {
                prompt.AppendLine($"Current product context: {productDetails.ProductName}");

                if (productDetails.AllAttributes?.Any() == true)
                {
                    prompt.AppendLine("Available specifications:");
                    foreach (var attr in productDetails.AllAttributes.Take(5))
                    {
                        prompt.AppendLine($"- {attr.Key}: {attr.Value}");
                    }
                }
            }

            prompt.AppendLine($"Query: {query}");
            prompt.AppendLine("Response:");

            return prompt.ToString();
        }

        private string BuildProductValidationPrompt(ProductName productName, List<ProductName> availableProducts)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("Find the best matching SKF bearing product from the available list.");
            prompt.AppendLine($"User requested: {productName}");
            prompt.AppendLine("Available products:");

            foreach (var product in availableProducts.Take(50))
            {
                prompt.AppendLine($"- {product}");
            }

            prompt.AppendLine("Best match:");

            return prompt.ToString();
        }
    }
}
