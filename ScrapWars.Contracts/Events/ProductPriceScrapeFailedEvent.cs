namespace ScrapWars.Contracts.Events;

public class ProductPriceScrapeFailedEvent
{
    public Guid EventId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public Guid? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public ulong GuildId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductUrl { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
