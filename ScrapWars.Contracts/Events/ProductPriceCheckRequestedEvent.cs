namespace ScrapWars.Contracts.Events;

public class ProductPriceCheckRequestedEvent
{
    public Guid EventId { get; set; }
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
    public Guid ProductId { get; set; }
    public Guid? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public ulong GuildId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductUrl { get; set; } = string.Empty;
    public decimal? LastKnownPrice { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
}
