using Microsoft.Extensions.Options;
using ScrapWars.Contracts.Events;

namespace ScrapWars.Scraper.Worker.Scraping;

public class ProductPriceScrapingService
{
    private readonly ISiteScraperRegistry _siteScraperRegistry;
    private readonly IOptions<ScrapingOptions> _scrapingOptions;

    public ProductPriceScrapingService(
        ISiteScraperRegistry siteScraperRegistry,
        IOptions<ScrapingOptions> scrapingOptions)
    {
        _siteScraperRegistry = siteScraperRegistry;
        _scrapingOptions = scrapingOptions;
    }

    public async Task<ProductPriceScrapedEvent> ScrapeAsync(
        ProductPriceCheckRequestedEvent request,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(request.ProductUrl, UriKind.Absolute, out var productUri))
        {
            throw new InvalidOperationException($"Invalid product URL '{request.ProductUrl}'.");
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(_scrapingOptions.Value.RequestTimeoutSeconds));
        using var linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        var scraper = _siteScraperRegistry.GetRequiredScraper(productUri);
        var result = await scraper.ScrapeAsync(productUri, linkedCancellationToken.Token);

        return new ProductPriceScrapedEvent
        {
            EventId = Guid.NewGuid(),
            CorrelationId = request.CorrelationId,
            ProductId = request.ProductId,
            CategoryId = request.CategoryId,
            CategoryName = request.CategoryName,
            GuildId = request.GuildId,
            ProductName = request.ProductName,
            ProductUrl = request.ProductUrl,
            SiteName = result.SiteName,
            BusinessType = result.BusinessType,
            LastKnownPrice = request.LastKnownPrice,
            CurrentPrice = result.CurrentPrice,
            DiscountPercentage = result.DiscountPercentage,
            Currency = result.Currency,
            ScrapedAtUtc = result.CapturedAtUtc
        };
    }
}
