namespace DevOpsDemo.Domain.Models
{
    public class Discount
    {
        public string Id { get; set; } = null!;

        public string ProductId { get; set; } = string.Empty;

        public decimal Percent { get; set; }
    }
}