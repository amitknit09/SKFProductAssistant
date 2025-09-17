using SKFProductAssistant.Domain.Entities;
using SKFProductAssistant.Domain.ValueObjects;

namespace SKFProductAssistant.Application.Interfaces
{
    public interface IProductService
    {
        Task<Product?> GetProductByNameAsync(ProductName productName);
        Task<List<ProductName>> GetAllProductNamesAsync();
        Task<List<ProductName>> FindSimilarProductsAsync(ProductName productName);
        Task<bool> ProductExistsAsync(ProductName productName);
    }
}
