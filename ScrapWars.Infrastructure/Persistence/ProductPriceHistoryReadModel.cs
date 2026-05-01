namespace ScrapWars.Infrastructure.Persistence;

public class ProductPriceHistoryReadModel
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime CapturedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
