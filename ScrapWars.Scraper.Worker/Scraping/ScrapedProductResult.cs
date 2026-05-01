using ScrapWars.Contracts.Events;

namespace ScrapWars.Scraper.Worker.Scraping;

public class ScrapedProductResult
{
    public string SiteName { get; set; } = string.Empty;
    public ListingBusinessType BusinessType { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
}
