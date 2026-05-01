namespace ScrapWars.Scraper.Worker.Scraping;

public class SiteScraperRegistry : ISiteScraperRegistry
{
    private readonly IReadOnlyCollection<ISiteScraper> _scrapers;

    public SiteScraperRegistry(IEnumerable<ISiteScraper> scrapers)
    {
        _scrapers = scrapers.ToArray();
    }

    public ISiteScraper GetRequiredScraper(Uri productUri)
    {
        var scraper = _scrapers.FirstOrDefault(candidate => candidate.CanHandle(productUri));

        return scraper
            ?? throw new UnsupportedSiteException(
                $"No scraper is registered for '{productUri.Host}'. Add a site-specific scraper before publishing price-check requests for that host.");
    }

    public IReadOnlyCollection<string> GetRegisteredSiteNames()
    {
        return _scrapers.Select(scraper => scraper.SiteName).OrderBy(name => name).ToArray();
    }
}
