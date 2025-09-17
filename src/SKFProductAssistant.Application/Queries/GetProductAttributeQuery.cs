using MediatR;
using SKFProductAssistant.Application.DTOs;

namespace SKFProductAssistant.Application.Queries
{
    public class GetProductAttributeQuery : IRequest<ProductAttributeResponseDto>
    {
        public string ProductName { get; set; } = string.Empty;
        public string AttributeName { get; set; } = string.Empty;

        public GetProductAttributeQuery(string productName, string attributeName)
        {
            ProductName = productName;
            AttributeName = attributeName;
        }
    }
}