// Infrastructure/Repositories/ProductRepository.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SKFProductAssistant.Domain.Entities;
using SKFProductAssistant.Domain.Interfaces;
using SKFProductAssistant.Domain.ValueObjects;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SKFProductAssistant.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ILogger<ProductRepository> _logger;
        private readonly string _dataPath;
        private List<Product>? _cachedProducts;
        private readonly SemaphoreSlim _loadSemaphore = new(1, 1);

        public ProductRepository(IConfiguration configuration, ILogger<ProductRepository> logger)
        {
            _logger = logger;
            _dataPath = configuration["DataPath"] ?? "Data";
        }

        public async Task<Product?> GetByIdAsync(ProductId productId)
        {
            await LoadDataAsync();
            return _cachedProducts?.FirstOrDefault(p => p.Id == productId);
        }

        public async Task<Product?> GetByNameAsync(ProductName productName)
        {
            await LoadDataAsync();
            return _cachedProducts?.FirstOrDefault(p =>
                string.Equals(p.Name.Value, productName.Value, StringComparison.OrdinalIgnoreCase) ||
                IsProductSimilar(p.Name.Value, productName.Value));
        }

        public async Task<List<Product>> GetAllAsync()
        {
            await LoadDataAsync();
            return _cachedProducts ?? new List<Product>();
        }

        public async Task<List<ProductName>> GetAllProductNamesAsync()
        {
            await LoadDataAsync();
            return _cachedProducts?.Select(p => p.Name).ToList() ?? new List<ProductName>();
        }

        public async Task<List<Product>> FindSimilarProductsAsync(ProductName productName)
        {
            await LoadDataAsync();

            return _cachedProducts?
                .Where(p => IsProductSimilar(p.Name.Value, productName.Value))
                .Take(5)
                .ToList() ?? new List<Product>();
        }

        public async Task<bool> ExistsAsync(ProductId productId)
        {
            var product = await GetByIdAsync(productId);
            return product != null;
        }

        private async Task LoadDataAsync()
        {
            if (_cachedProducts != null) return;

            await _loadSemaphore.WaitAsync();
            try
            {
                if (_cachedProducts != null) return;

                _cachedProducts = new List<Product>();

                var dataFiles = Directory.GetFiles(_dataPath, "*.json");

                foreach (var file in dataFiles)
                {
                    try
                    {
                        var jsonContent = await File.ReadAllTextAsync(file);
                        var rawData = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(jsonContent);

                        if (rawData != null)
                        {
                            var products = rawData.Select(ConvertToProduct).Where(p => p != null);
                            _cachedProducts.AddRange(products!);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading datasheet: {File}", file);
                    }
                }

                _logger.LogInformation("Loaded {Count} products from datasheets", _cachedProducts.Count);
            }
            finally
            {
                _loadSemaphore.Release();
            }
        }

        private Product? ConvertToProduct(Dictionary<string, JsonElement> rawProduct)
        {
            try
            {
                // Get product name - FIXED: Use correct JsonElement methods
                var productNameField = rawProduct.Keys.FirstOrDefault(k =>
                    k.Equals("ProductName", StringComparison.OrdinalIgnoreCase) ||
                    k.Equals("Product", StringComparison.OrdinalIgnoreCase) ||
                    k.Equals("Name", StringComparison.OrdinalIgnoreCase));

                if (productNameField == null)
                    return null;

                var productNameElement = rawProduct[productNameField];

                // FIXED: Check ValueKind first, then use GetString()
                if (productNameElement.ValueKind != JsonValueKind.String)
                    return null;

                var productNameValue = productNameElement.GetString();
                if (string.IsNullOrEmpty(productNameValue))
                    return null;

                var productName = new ProductName(productNameValue);
                var productId = new ProductId(productNameValue);

                // Convert attributes
                var attributes = new Dictionary<string, ProductAttribute>();
                foreach (var kvp in rawProduct.Where(kv => kv.Key != productNameField))
                {
                    var attributeValue = GetStringValue(kvp.Value);
                    if (!string.IsNullOrEmpty(attributeValue))
                    {
                        var (value, unit) = ParseValueAndUnit(attributeValue);
                        var attributeType = DetermineAttributeType(kvp.Key, value, unit);

                        attributes[kvp.Key.ToLowerInvariant()] = new ProductAttribute(
                            kvp.Key, value, unit, attributeType);
                    }
                }

                return new Product(productId, productName, attributes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting raw product data");
                return null;
            }
        }

        // FIXED: Correct JsonElement handling
        private string GetStringValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? "",
                JsonValueKind.Number => element.GetRawText(), // Use GetRawText() for numbers
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => "",
                _ => element.GetRawText() // Fallback to raw text representation
            };
        }

        private (string value, string unit) ParseValueAndUnit(string input)
        {
            var match = Regex.Match(input, @"^([\d.,]+)\s*([a-zA-Z]*)$");

            if (match.Success)
            {
                return (match.Groups[1].Value, match.Groups[2].Value);
            }

            return (input, "");
        }

        private Domain.Enums.AttributeType DetermineAttributeType(string attributeName, string value, string unit)
        {
            var lowerName = attributeName.ToLowerInvariant();

            if (lowerName.Contains("diameter") || lowerName.Contains("width") || lowerName.Contains("height"))
                return Domain.Enums.AttributeType.Dimension;

            if (lowerName.Contains("load") || lowerName.Contains("capacity"))
                return Domain.Enums.AttributeType.Load;

            if (lowerName.Contains("speed") || lowerName.Contains("rpm"))
                return Domain.Enums.AttributeType.Speed;

            if (lowerName.Contains("mass") || lowerName.Contains("weight"))
                return Domain.Enums.AttributeType.Mass;

            if (double.TryParse(value, out _))
                return Domain.Enums.AttributeType.Numeric;

            return Domain.Enums.AttributeType.Text;
        }

        private bool IsProductSimilar(string product1, string product2)
        {
            if (string.IsNullOrEmpty(product1) || string.IsNullOrEmpty(product2))
                return false;

            var p1 = product1.ToLowerInvariant().Replace(" ", "").Replace("-", "");
            var p2 = product2.ToLowerInvariant().Replace(" ", "").Replace("-", "");

            return p1.Contains(p2) || p2.Contains(p1) ||
                   LevenshteinDistance(p1, p2) <= 2;
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
