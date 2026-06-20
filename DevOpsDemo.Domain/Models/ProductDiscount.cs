public class ProductDiscount
{
    public string? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? Category { get; set; }
    public decimal? Price { get; set; }

    public string? DiscountId { get; set; }
    public decimal? Percent { get; set; }
}