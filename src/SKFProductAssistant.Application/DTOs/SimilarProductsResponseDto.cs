namespace SKFProductAssistant.Application.DTOs
{
    public class SimilarProductsResponseDto
    {
        public List<string> SimilarProducts { get; set; } = new();
        public bool HasSuggestions => SimilarProducts.Any();
    }
}