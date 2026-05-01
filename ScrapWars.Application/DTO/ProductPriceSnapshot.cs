namespace ScrapWars.Application.DTO;

public class ProductPriceSnapshot
{
    public Guid ProductId { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime CapturedAtUtc { get; set; }
}
