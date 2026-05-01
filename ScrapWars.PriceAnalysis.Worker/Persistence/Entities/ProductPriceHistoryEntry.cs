using ScrapWars.Contracts.Events;

namespace ScrapWars.PriceAnalysis.Worker.Persistence.Entities;

public class ProductPriceHistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceEventId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public ulong GuildId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductUrl { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public ListingBusinessType BusinessType { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime CapturedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
