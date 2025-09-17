using SKFProductAssistant.Domain.Entities;
using SKFProductAssistant.Domain.ValueObjects;

namespace SKFProductAssistant.Domain.Interfaces
{
    public interface IConversationRepository
    {
        Task<Conversation?> GetByIdAsync(ConversationId conversationId);
        Task SaveAsync(Conversation conversation);
        Task DeleteAsync(ConversationId conversationId);
        Task CleanupExpiredAsync(TimeSpan timeout);
    }
}