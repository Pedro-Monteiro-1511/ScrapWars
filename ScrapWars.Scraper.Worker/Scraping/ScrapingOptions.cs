namespace ScrapWars.Scraper.Worker.Scraping;

public class ScrapingOptions
{
    public const string SectionName = "Scraping";

    public int RequestTimeoutSeconds { get; set; } = 30;
}
