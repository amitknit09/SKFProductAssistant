using SKFProductAssistant.Domain.Enums;

namespace SKFProductAssistant.Application.DTOs
{
    public class ProductQueryResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Answer { get; set; }
        public string? ConversationId { get; set; }
        public List<string>? Suggestions { get; set; }
        public ProductDetailsDto? ProductDetails { get; set; }
        public QueryResultType ResultType { get; set; }
    }
}

