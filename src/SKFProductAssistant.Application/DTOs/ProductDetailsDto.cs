namespace SKFProductAssistant.Application.DTOs
{
    public class ProductDetailsDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string Attribute { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public Dictionary<string, string> AllAttributes { get; set; } = new();
    }
}
