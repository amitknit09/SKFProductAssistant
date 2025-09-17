// Infrastructure/Services/ProductSimilarityService.cs
using SKFProductAssistant.Domain.Interfaces;
using SKFProductAssistant.Domain.ValueObjects;

namespace SKFProductAssistant.Infrastructure.Services
{
    public class ProductSimilarityService : IProductSimilarityService
    {
        public async Task<List<ProductName>> FindSimilarProductNamesAsync(ProductName productName, List<ProductName> availableProducts)
        {
            return await Task.FromResult(
                availableProducts
                    .Where(p => CalculateSimilarity(p.Value, productName.Value) > 0.7)
                    .OrderByDescending(p => CalculateSimilarity(p.Value, productName.Value))
                    .Take(5)
                    .ToList()
            );
        }

        public double CalculateSimilarity(string product1, string product2)
        {
            if (string.IsNullOrEmpty(product1) || string.IsNullOrEmpty(product2))
                return 0;

            var p1 = product1.ToLowerInvariant().Replace(" ", "").Replace("-", "");
            var p2 = product2.ToLowerInvariant().Replace(" ", "").Replace("-", "");

            if (p1 == p2) return 1.0;
            if (p1.Contains(p2) || p2.Contains(p1)) return 0.8;

            var distance = LevenshteinDistance(p1, p2);
            var maxLength = Math.Max(p1.Length, p2.Length);

            return maxLength == 0 ? 0 : 1.0 - (double)distance / maxLength;
        }

        private int LevenshteinDistance(string s1, string s2)
        {
            var matrix = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                matrix[i, 0] = i;

            for (int j = 0; j <= s2.Length; j++)
                matrix[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    int cost = (s2[j - 1] == s1[i - 1]) ? 0 : 1;
                    matrix[i, j] = Math.Min(Math.Min(
                        matrix[i - 1, j] + 1,
                        matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[s1.Length, s2.Length];
        }
    }
}
