using MediatR;
using Microsoft.Extensions.Logging;
using SKFProductAssistant.Application.DTOs;
using SKFProductAssistant.Application.Interfaces;
using SKFProductAssistant.Application.Queries;
using SKFProductAssistant.Domain.Enums;

namespace SKFProductAssistant.Application.Handlers
{
    public class ProcessProductQueryQueryHandler : IRequestHandler<ProcessProductQueryQuery, ProductQueryResponseDto>
    {
        private readonly IProductQueryService _productQueryService;
        private readonly IConversationService _conversationService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<ProcessProductQueryQueryHandler> _logger;

        public ProcessProductQueryQueryHandler(
            IProductQueryService productQueryService,
            IConversationService conversationService,
            ICacheService cacheService,
            ILogger<ProcessProductQueryQueryHandler> logger)
        {
            _productQueryService = productQueryService;
            _conversationService = conversationService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<ProductQueryResponseDto> Handle(ProcessProductQueryQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return new ProductQueryResponseDto
                    {
                        Success = false,
                        Message = "Query cannot be empty",
                        ResultType = QueryResultType.InvalidQuery
                    };
                }

                // Generate cache key
                var cacheKey = GenerateCacheKey(request.Query, request.ConversationId);

                // Check cache
                var cachedResponse = await _cacheService.GetAsync<ProductQueryResponseDto>(cacheKey);
                if (cachedResponse != null)
                {
                    _logger.LogInformation("Cache hit for query: {Query}", request.Query);
                    return cachedResponse;
                }

                // Load or create conversation
                var conversation = await _conversationService.GetOrCreateConversationAsync(request.ConversationId);

                // Process the query
                var result = await _productQueryService.ProcessQueryAsync(request.Query, conversation);

                // Update conversation
                conversation.AddQuery(request.Query, result.Answer);
                await _conversationService.SaveConversationAsync(conversation);

                // Set conversation ID in response
                result.ConversationId = conversation.Id;

                // Cache successful responses
                if (result.Success)
                {
                    await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(6));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing query: {Query}", request.Query);
                return new ProductQueryResponseDto
                {
                    Success = false,
                    Message = "An error occurred while processing your query",
                    ConversationId = request.ConversationId,
                    ResultType = QueryResultType.SystemError
                };
            }
        }

        private string GenerateCacheKey(string query, string? conversationId)
        {
            var normalizedQuery = query.ToLowerInvariant().Trim();
            var hash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(normalizedQuery))[..16];
            return $"query:{hash}:{conversationId}";
        }
    }
}