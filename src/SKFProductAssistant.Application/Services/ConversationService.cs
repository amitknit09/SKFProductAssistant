using SKFProductAssistant.Application.Interfaces;
using SKFProductAssistant.Domain.Entities;
using SKFProductAssistant.Domain.Interfaces;
using SKFProductAssistant.Domain.ValueObjects;

namespace SKFProductAssistant.Application.Services
{
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _conversationRepository;

        public ConversationService(IConversationRepository conversationRepository)
        {
            _conversationRepository = conversationRepository;
        }

        public async Task<Conversation> GetOrCreateConversationAsync(string? conversationId)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                return new Conversation(ConversationId.NewId());
            }

            var id = new ConversationId(conversationId);
            var conversation = await _conversationRepository.GetByIdAsync(id);

            return conversation ?? new Conversation(id);
        }

        public async Task<Conversation?> GetConversationAsync(ConversationId conversationId)
        {
            return await _conversationRepository.GetByIdAsync(conversationId);
        }

        public async Task SaveConversationAsync(Conversation conversation)
        {
            await _conversationRepository.SaveAsync(conversation);
        }

        public async Task DeleteConversationAsync(ConversationId conversationId)
        {
            await _conversationRepository.DeleteAsync(conversationId);
        }
    }
}
