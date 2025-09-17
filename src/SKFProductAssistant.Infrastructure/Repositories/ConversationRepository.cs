using Microsoft.Extensions.Logging;
using SKFProductAssistant.Application.Interfaces;
using SKFProductAssistant.Domain.Entities;
using SKFProductAssistant.Domain.Interfaces;
using SKFProductAssistant.Domain.ValueObjects;

namespace SKFProductAssistant.Infrastructure.Repositories
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<ConversationRepository> _logger;

        public ConversationRepository(ICacheService cacheService, ILogger<ConversationRepository> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Conversation?> GetByIdAsync(ConversationId conversationId)
        {
            try
            {
                return await _cacheService.GetAsync<Conversation>($"conversation:{conversationId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation: {ConversationId}", conversationId);
                return null;
            }
        }

        public async Task SaveAsync(Conversation conversation)
        {
            try
            {
                await _cacheService.SetAsync(
                    $"conversation:{conversation.Id}",
                    conversation,
                    TimeSpan.FromHours(24));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving conversation: {ConversationId}", conversation.Id);
            }
        }

        public async Task DeleteAsync(ConversationId conversationId)
        {
            try
            {
                await _cacheService.RemoveAsync($"conversation:{conversationId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting conversation: {ConversationId}", conversationId);
            }
        }

        public async Task CleanupExpiredAsync(TimeSpan timeout)
        {
            try
            {
                // This would require a more sophisticated cache implementation
                // For now, conversations expire automatically via cache TTL
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired conversations");
            }
        }
    }
}
