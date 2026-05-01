namespace ScrapWars.Infrastructure.Messaging;

public class RabbitMqTopologyOptions
{
    public const string SectionName = "RabbitMqTopology";

    public string PriceCheckRequestedQueue { get; set; } = "scrapwars.scraper.price-check.requested";
}
