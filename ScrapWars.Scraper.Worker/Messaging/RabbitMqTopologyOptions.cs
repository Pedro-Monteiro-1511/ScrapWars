namespace ScrapWars.Scraper.Worker.Messaging;

public class RabbitMqTopologyOptions
{
    public const string SectionName = "RabbitMqTopology";

    public string PriceCheckRequestedQueue { get; set; } = "scrapwars.scraper.price-check.requested";
    public string PriceScrapedExchange { get; set; } = "scrapwars.events.product-price-scraped";
    public string PriceScrapedRoutingKey { get; set; } = "product.price-scraped";
    public string PriceScrapeFailedExchange { get; set; } = "scrapwars.events.product-price-scrape-failed";
    public string PriceScrapeFailedRoutingKey { get; set; } = "product.price-scrape-failed";
}
