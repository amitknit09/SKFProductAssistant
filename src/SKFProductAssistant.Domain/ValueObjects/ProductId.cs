// Domain/ValueObjects/ProductId.cs
namespace SKFProductAssistant.Domain.ValueObjects
{
    public class ProductId : ValueObject
    {
        public string Value { get; private set; }

        private ProductId() { } // For EF Core

        public ProductId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Product ID cannot be empty", nameof(value));

            Value = value.Trim().ToUpperInvariant();
        }

        public static implicit operator string(ProductId productId) => productId.Value;
        public static implicit operator ProductId(string value) => new(value);

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value;
    }
}





