using MediatR;
using SKFProductAssistant.Application.Commands;
using SKFProductAssistant.Application.DTOs;
using SKFProductAssistant.Application.Interfaces;
using SKFProductAssistant.Application.Queries;
using SKFProductAssistant.Domain.Entities;
using SKFProductAssistant.Domain.Enums;
using SKFProductAssistant.Domain.ValueObjects;

namespace SKFProductAssistant.Application.Handlers
{
    public class StartConversationCommandHandler : IRequestHandler<StartConversationCommand, ConversationResponseDto>
    {
        private readonly IConversationService _conversationService;

        public StartConversationCommandHandler(IConversationService conversationService)
        {
            _conversationService = conversationService;
        }

        public async Task<ConversationResponseDto> Handle(StartConversationCommand request, CancellationToken cancellationToken)
        {
            var conversationId = ConversationId.NewId();
            var conversation = new Conversation(conversationId);

            await _conversationService.SaveConversationAsync(conversation);

            return new ConversationResponseDto
            {
                ConversationId = conversation.Id,
                CreatedAt = conversation.CreatedAt
            };
        }
    }
}