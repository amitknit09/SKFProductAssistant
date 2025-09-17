using MediatR;
using SKFProductAssistant.Application.DTOs;
using SKFProductAssistant.Application.Interfaces;
using SKFProductAssistant.Application.Queries;
using SKFProductAssistant.Domain.ValueObjects;

namespace SKFProductAssistant.Application.Handlers
{
    public class GetProductAttributeQueryHandler : IRequestHandler<GetProductAttributeQuery, ProductAttributeResponseDto>
    {
        private readonly IProductService _productService;

        public GetProductAttributeQueryHandler(IProductService productService)
        {
            _productService = productService;
        }

        public async Task<ProductAttributeResponseDto> Handle(GetProductAttributeQuery request, CancellationToken cancellationToken)
        {
            var productName = new ProductName(request.ProductName);
            var product = await _productService.GetProductByNameAsync(productName);

            if (product == null)
            {
                return new ProductAttributeResponseDto
                {
                    Found = false,
                    AvailableAttributes = new List<string>()
                };
            }

            var attribute = product.GetAttribute(request.AttributeName);

            var response = new ProductAttributeResponseDto
            {
                Found = attribute != null,
                AvailableAttributes = product.GetAvailableAttributes()
            };

            if (attribute != null)
            {
                response.ProductDetails = new ProductDetailsDto
                {
                    ProductName = product.Name,
                    Attribute = attribute.Name,
                    Value = attribute.Value,
                    Unit = attribute.Unit,
                    AllAttributes = product.Attributes.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.GetFormattedValue()
                    )
                };
            }

            return response;
        }
    }
}