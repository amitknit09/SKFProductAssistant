using SKFProductAssistant.Domain.Enums;

namespace SKFProductAssistant.Domain.ValueObjects
{
    public class ProductAttribute : ValueObject
    {
        public string Name { get; private set; }
        public string Value { get; private set; }
        public string Unit { get; private set; }
        public AttributeType Type { get; private set; }

        private ProductAttribute() { } // For EF Core

        public ProductAttribute(string name, string value, string unit = "", AttributeType type = AttributeType.Text)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Unit = unit ?? string.Empty;
            Type = type;
        }

        public string GetFormattedValue()
        {
            return string.IsNullOrEmpty(Unit) ? Value : $"{Value} {Unit}";
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
            yield return Value;
            yield return Unit;
            yield return Type;
        }
    }
}