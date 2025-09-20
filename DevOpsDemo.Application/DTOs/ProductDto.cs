namespace DevOpsDemo.Application.DTOs
{
    public class ProductDto
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Category { get; set; } = null!;
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
