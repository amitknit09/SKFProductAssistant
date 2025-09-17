namespace SKFProductAssistant.Domain.ValueObjects
{
    public class ProductName : ValueObject
    {
        public string Value { get; private set; }

        private ProductName() { } // For EF Core

        public ProductName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Product name cannot be empty", nameof(value));

            Value = value.Trim();
        }

        public static implicit operator string(ProductName name) => name.Value;
        public static implicit operator ProductName(string value) => new(value);

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value;
    }
}
