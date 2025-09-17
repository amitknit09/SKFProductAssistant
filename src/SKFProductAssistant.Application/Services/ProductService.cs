// Application/Services/ProductService.cs
using SKFProductAssistant.Application.Interfaces;
using SKFProductAssistant.Domain.Entities;
using SKFProductAssistant.Domain.Interfaces;
using SKFProductAssistant.Domain.ValueObjects;

namespace SKFProductAssistant.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<Product?> GetProductByNameAsync(ProductName productName)
        {
            return await _productRepository.GetByNameAsync(productName);
        }

        public async Task<List<ProductName>> GetAllProductNamesAsync()
        {
            return await _productRepository.GetAllProductNamesAsync();
        }

        public async Task<List<ProductName>> FindSimilarProductsAsync(ProductName productName)
        {
            var similarProducts = await _productRepository.FindSimilarProductsAsync(productName);
            return similarProducts.Select(p => p.Name).ToList();
        }

        public async Task<bool> ProductExistsAsync(ProductName productName)
        {
            var product = await _productRepository.GetByNameAsync(productName);
            return product != null;
        }
    }
}
