// Domain/Entities/Product.cs
using SKFProductAssistant.Domain.ValueObjects;

namespace SKFProductAssistant.Domain.Entities
{
    public class Product
    {
        public ProductId Id { get; private set; }
        public ProductName Name { get; private set; }
        public Dictionary<string, ProductAttribute> Attributes { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        private Product() { } // For EF Core

        public Product(ProductId id, ProductName name, Dictionary<string, ProductAttribute> attributes)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Attributes = attributes ?? new Dictionary<string, ProductAttribute>();
            CreatedAt = DateTime.UtcNow;
        }

        public ProductAttribute? GetAttribute(string attributeName)
        {
            return Attributes.TryGetValue(attributeName.ToLowerInvariant(), out var attribute)
                ? attribute
                : null;
        }

        public bool HasAttribute(string attributeName)
        {
            return Attributes.ContainsKey(attributeName.ToLowerInvariant());
        }

        public void UpdateAttribute(string attributeName, ProductAttribute attribute)
        {
            Attributes[attributeName.ToLowerInvariant()] = attribute;
            UpdatedAt = DateTime.UtcNow;
        }

        public List<string> GetAvailableAttributes()
        {
            return Attributes.Keys.ToList();
        }
    }
}
