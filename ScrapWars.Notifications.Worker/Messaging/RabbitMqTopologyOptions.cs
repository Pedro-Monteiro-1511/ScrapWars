namespace ScrapWars.Notifications.Worker.Messaging;

public class RabbitMqTopologyOptions
{
    public const string SectionName = "RabbitMqTopology";

    public string DealDetectedExchange { get; set; } = "scrapwars.events.product-deal-detected";
    public string DealDetectedQueue { get; set; } = "scrapwars.notifications.product-deal-detected";
}
