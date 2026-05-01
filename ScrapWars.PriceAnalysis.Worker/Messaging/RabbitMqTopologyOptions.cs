namespace ScrapWars.PriceAnalysis.Worker.Messaging;

public class RabbitMqTopologyOptions
{
    public const string SectionName = "RabbitMqTopology";

    public string PriceScrapedExchange { get; set; } = "scrapwars.events.product-price-scraped";
    public string PriceScrapedQueue { get; set; } = "scrapwars.analysis.product-price-scraped";
    public string DealDetectedExchange { get; set; } = "scrapwars.events.product-deal-detected";
    public string DealDetectedRoutingKey { get; set; } = "product.deal-detected";
    public string DealDetectionFailedExchange { get; set; } = "scrapwars.events.product-deal-detection-failed";
    public string DealDetectionFailedRoutingKey { get; set; } = "product.deal-detection-failed";
}
