using MediatR;
using SKFProductAssistant.Application.DTOs;

namespace SKFProductAssistant.Application.Queries
{
    public class ProcessProductQueryQuery : IRequest<ProductQueryResponseDto>
    {
        public string Query { get; set; } = string.Empty;
        public string? ConversationId { get; set; }

        public ProcessProductQueryQuery(string query, string? conversationId = null)
        {
            Query = query;
            ConversationId = conversationId;
        }
    }
}