namespace ScrapWars.Scraper.Worker.Scraping;

public interface ISiteScraper
{
    string SiteName { get; }
    bool CanHandle(Uri productUri);
    Task<ScrapedProductResult> ScrapeAsync(Uri productUri, CancellationToken cancellationToken = default);
}
