using SKFProductAssistant.Application.DTOs;
using SKFProductAssistant.Domain.Entities;
using SKFProductAssistant.Domain.ValueObjects;

namespace SKFProductAssistant.Application.Interfaces
{
    public interface IAIService
    {
        Task<ProductName?> ExtractProductNameAsync(string query, Conversation? conversation = null);
        Task<string?> ExtractAttributeAsync(string query, Conversation? conversation = null);
        Task<string> GenerateResponseAsync(ProductName productName, string attribute, string value, string unit, Conversation? conversation = null);
        Task<ProductName?> ValidateProductExistenceAsync(ProductName productName, List<ProductName> availableProducts);
        Task<string> GenerateConversationalResponseAsync(string query, Conversation conversation, ProductDetailsDto? productInfo = null);
    }
}
