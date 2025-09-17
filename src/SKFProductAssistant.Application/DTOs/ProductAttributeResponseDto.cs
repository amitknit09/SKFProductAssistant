namespace SKFProductAssistant.Application.DTOs
{
    public class ProductAttributeResponseDto
    {
        public bool Found { get; set; }
        public ProductDetailsDto? ProductDetails { get; set; }
        public List<string> AvailableAttributes { get; set; } = new();
    }
}