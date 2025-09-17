using MediatR;
using SKFProductAssistant.Application.DTOs;

namespace SKFProductAssistant.Application.Queries
{
    public class GetSimilarProductsQuery : IRequest<SimilarProductsResponseDto>
    {
        public string ProductName { get; set; } = string.Empty;

        public GetSimilarProductsQuery(string productName)
        {
            ProductName = productName;
        }
    }
}