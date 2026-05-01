namespace ScrapWars.Infrastructure.Services;

public class ScheduledPriceCheckOptions
{
    public const string SectionName = "ScheduledPriceChecks";

    public bool Enabled { get; set; } = true;
    public string TimeZoneId { get; set; } = "Europe/Lisbon";
}
