namespace ScrapWars.Scraper.Worker.Scraping;

public interface ISiteScraperRegistry
{
    ISiteScraper GetRequiredScraper(Uri productUri);
    IReadOnlyCollection<string> GetRegisteredSiteNames();
}
