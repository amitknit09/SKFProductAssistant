using SKFProductAssistant.Domain.Entities;
using SKFProductAssistant.Domain.ValueObjects;

namespace SKFProductAssistant.Application.Interfaces
{
    public interface IConversationService
    {
        Task<Conversation> GetOrCreateConversationAsync(string? conversationId);
        Task<Conversation?> GetConversationAsync(ConversationId conversationId);
        Task SaveConversationAsync(Conversation conversation);
        Task DeleteConversationAsync(ConversationId conversationId);
    }
}
