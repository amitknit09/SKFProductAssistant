using SKFProductAssistant.Application.DTOs;
using SKFProductAssistant.Domain.Entities;
using SKFProductAssistant.Domain.ValueObjects;

namespace SKFProductAssistant.Application.Interfaces
{
    public interface IProductQueryService
    {
        Task<ProductQueryResponseDto> ProcessQueryAsync(string query, Conversation conversation);
    }
}
