// Domain/Interfaces/IProductRepository.cs
using SKFProductAssistant.Domain.Entities;
using SKFProductAssistant.Domain.ValueObjects;

namespace SKFProductAssistant.Domain.Interfaces
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(ProductId productId);
        Task<Product?> GetByNameAsync(ProductName productName);
        Task<List<Product>> GetAllAsync();
        Task<List<ProductName>> GetAllProductNamesAsync();
        Task<List<Product>> FindSimilarProductsAsync(ProductName productName);
        Task<bool> ExistsAsync(ProductId productId);
    }
}
