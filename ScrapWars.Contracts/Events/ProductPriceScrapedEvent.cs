namespace ScrapWars.Contracts.Events;

public class ProductPriceScrapedEvent
{
    public Guid EventId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public Guid? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public ulong GuildId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductUrl { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public ListingBusinessType BusinessType { get; set; }
    public decimal? LastKnownPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime ScrapedAtUtc { get; set; } = DateTime.UtcNow;
}
