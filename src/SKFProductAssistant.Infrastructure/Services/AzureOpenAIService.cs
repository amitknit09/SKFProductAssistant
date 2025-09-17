// Infrastructure/Services/AzureOpenAIService.cs
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SKFProductAssistant.Application.DTOs;
using SKFProductAssistant.Application.Interfaces;
using SKFProductAssistant.Domain.Entities;
using SKFProductAssistant.Domain.ValueObjects;
using System.Text.Json;

namespace SKFProductAssistant.Infrastructure.Services
{
    public class AzureOpenAIService : IAIService
    {
        private readonly OpenAIClient _openAIClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureOpenAIService> _logger;
        private readonly string _deploymentName;

        public AzureOpenAIService(IConfiguration configuration, ILogger<AzureOpenAIService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var endpoint = _configuration["AzureOpenAI:Endpoint"];
            var apiKey = _configuration["AzureOpenAI:ApiKey"];
            _deploymentName = _configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4";

            _openAIClient = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        }

        public async Task<ProductName?> ExtractProductNameAsync(string query, Conversation? conversation = null)
        {
            var contextPrompt = BuildContextPrompt(conversation);

            var prompt = $@"
{contextPrompt}

Extract the SKF product designation from this query. Be very precise and only return product designations that follow SKF naming conventions.

Query: '{query}'

SKF product designations typically follow patterns like:
- 6205, 6206, 6207 (deep groove ball bearings)  
- 6205-2RS1, 6206-Z (with suffixes)
- NU 205, NU 206 (cylindrical roller bearings)
- 29320 E, 29322 E (spherical roller thrust bearings)

Rules:
- Return only the exact product designation
- If no valid SKF product is mentioned, return 'NONE'
- Do not guess or suggest products
- Consider conversation context if provided

Examples:
- 'What is the width of 6205?' → 6205
- 'Height of bearing 6206-2RS1?' → 6206-2RS1  
- 'Tell me about bearings' → NONE
- 'What about the 6207?' (with context showing previous discussion of 6205) → 6207
";

            var result = await GetCompletionAsync(prompt, temperature: 0.1f);
            return result == "NONE" ? null : new ProductName(result);
        }

        public async Task<string?> ExtractAttributeAsync(string query, Conversation? conversation = null)
        {
            var contextPrompt = BuildContextPrompt(conversation);

            var prompt = $@"
{contextPrompt}

Extract the specific attribute being requested from this query. Only return standard bearing attributes.

Query: '{query}'

Valid attributes (return exactly one):
- inner_diameter (or bore)
- outer_diameter 
- width (or thickness)
- dynamic_load_rating
- static_load_rating
- limiting_speed
- mass (or weight)

Rules:
- Return only one attribute name from the list above
- Use the exact terms listed
- If unclear or not a valid attribute, return 'NONE'
- Consider conversation context

Examples:
- 'What is the width of 6205?' → width
- 'Inner diameter?' → inner_diameter
- 'How heavy is it?' → mass
- 'Load capacity?' → dynamic_load_rating
";

            var result = await GetCompletionAsync(prompt, temperature: 0.1f);
            return result == "NONE" ? null : result;
        }

        public async Task<string> GenerateResponseAsync(ProductName productName, string attribute, string value, string unit, Conversation? conversation = null)
        {
            var contextPrompt = BuildContextPrompt(conversation);

            var prompt = $@"
{contextPrompt}

Generate a natural, professional response about this SKF bearing information:

Product: {productName}
Attribute: {attribute}
Value: {value}
Unit: {unit}

Requirements:
- Be conversational and helpful
- Include the specific product name
- Format numbers clearly
- Keep response concise (1-2 sentences)
- Consider conversation flow if context provided

Example: 'The inner diameter of the SKF 6205 bearing is 25mm.'
";

            return await GetCompletionAsync(prompt, temperature: 0.3f);
        }

        public async Task<bool> ValidateProductExistenceAsync(ProductName productName, List<ProductName> knownProducts)
        {
            if (knownProducts.Any(p => string.Equals(p.Value, productName.Value, StringComparison.OrdinalIgnoreCase)))
                return true;

            var prompt = $@"
Given this list of known SKF products: {string.Join(", ", knownProducts.Take(50).Select(p => p.Value))}

Is '{productName}' a valid match for any of these products? Consider:
- Exact matches
- Minor variations (spaces, dashes, case)
- Abbreviated forms

Return only 'YES' or 'NO'.
";

            var result = await GetCompletionAsync(prompt, temperature: 0.0f);
            return result.Trim().ToUpper() == "YES";
        }

        public async Task<string> GenerateConversationalResponseAsync(string query, Conversation conversation, ProductDetailsDto? productInfo = null)
        {
            var conversationHistory = string.Join("\n", conversation.GetRecentQueries().Select(q => $"- {q}"));

            var prompt = $@"
You are an SKF bearing specialist assistant. Generate a helpful, conversational response.

Current query: '{query}'
Conversation ID: {conversation.Id}
Recent queries:
{conversationHistory}

Last product discussed: {conversation.LastProductDiscussed?.Value ?? "None"}

";

            if (productInfo != null)
            {
                prompt += $@"
Found information:
- Product: {productInfo.ProductName}  
- {productInfo.Attribute}: {productInfo.Value} {productInfo.Unit}
";
            }

            prompt += @"
Requirements:
- Be helpful and professional
- Reference conversation context when relevant
- If no information found, offer alternatives or ask clarifying questions
- Keep responses concise but friendly
- Suggest related information when appropriate
";

            return await GetCompletionAsync(prompt, temperature: 0.4f);
        }

        private string BuildContextPrompt(Conversation? conversation)
        {
            if (conversation == null) return "";

            return $@"
Conversation context:
- Last product discussed: {conversation.LastProductDiscussed?.Value ?? "None"}
- Recent queries: {string.Join(", ", conversation.GetRecentQueries())}
";
        }

        private async Task<string> GetCompletionAsync(string prompt, float temperature = 0.3f)
        {
            try
            {
                var chatCompletionsOptions = new ChatCompletionsOptions
                {
                    DeploymentName = _deploymentName,
                    Messages = {
                        new ChatRequestSystemMessage("You are a precise SKF bearing information assistant. Be accurate and only provide information you're certain about."),
                        new ChatRequestUserMessage(prompt)
                    },
                    Temperature = temperature,
                    MaxTokens = 200
                };

                var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
                return response.Value.Choices[0].Message.Content.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Azure OpenAI");
                throw;
            }
        }
    }
}
