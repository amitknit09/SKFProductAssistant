using SKFProductAssistant.Domain.ValueObjects;

namespace SKFProductAssistant.Domain.Interfaces
{
    public interface IProductSimilarityService
    {
        Task<List<ProductName>> FindSimilarProductNamesAsync(ProductName productName, List<ProductName> availableProducts);
        double CalculateSimilarity(string product1, string product2);
    }
}
