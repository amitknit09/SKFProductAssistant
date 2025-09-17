using MediatR;
using SKFProductAssistant.Application.DTOs;

namespace SKFProductAssistant.Application.Commands
{
    public class StartConversationCommand : IRequest<ConversationResponseDto>
    {
        public StartConversationCommand() { }
    }
}
