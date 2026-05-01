namespace ScrapWars.Contracts.Events;

public class ProductDealDetectedEvent
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
    public decimal CurrentPrice { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public decimal? PreviousPrice { get; set; }
    public decimal? RecentWindowLowestPrice { get; set; }
    public decimal? HistoricalLowestPrice { get; set; }
    public int RecentWindowDays { get; set; }
    public int HistoricalWindowDays { get; set; }
    public string Currency { get; set; } = "EUR";
    public DealKind DealKind { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
