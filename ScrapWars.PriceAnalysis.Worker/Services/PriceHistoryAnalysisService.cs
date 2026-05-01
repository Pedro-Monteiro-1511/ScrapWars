using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScrapWars.Contracts.Events;
using ScrapWars.PriceAnalysis.Worker.Persistence;
using ScrapWars.PriceAnalysis.Worker.Persistence.Entities;

namespace ScrapWars.PriceAnalysis.Worker.Services;

public class PriceHistoryAnalysisService
{
    private readonly PriceHistoryDbContext _dbContext;
    private readonly IOptions<PriceAnalysisOptions> _options;

    public PriceHistoryAnalysisService(
        PriceHistoryDbContext dbContext,
        IOptions<PriceAnalysisOptions> options)
    {
        _dbContext = dbContext;
        _options = options;
    }

    public async Task<ProductDealDetectedEvent?> RecordAndAnalyzeAsync(
        ProductPriceScrapedEvent scrapedEvent,
        CancellationToken cancellationToken)
    {
        var alreadyRecorded = await _dbContext.ProductPriceHistory
            .AsNoTracking()
            .AnyAsync(item => item.SourceEventId == scrapedEvent.EventId, cancellationToken);

        if (alreadyRecorded)
        {
            return null;
        }

        var options = _options.Value;
        var recentWindowStart = scrapedEvent.ScrapedAtUtc.AddDays(-options.RecentWindowDays);
        var superDealWindowStart = scrapedEvent.ScrapedAtUtc.AddDays(-options.SuperDealWindowDays);

        var existingHistory = await _dbContext.ProductPriceHistory
            .AsNoTracking()
            .Where(item =>
                item.ProductId == scrapedEvent.ProductId &&
                item.CapturedAtUtc < scrapedEvent.ScrapedAtUtc)
            .OrderByDescending(item => item.CapturedAtUtc)
            .ToArrayAsync(cancellationToken);

        var previousPrice = existingHistory.FirstOrDefault()?.Price;
        var recentWindowLowestPrice = existingHistory
            .Where(item => item.CapturedAtUtc >= recentWindowStart)
            .Select(item => (decimal?)item.Price)
            .Min();
        var historicalLowestPrice = existingHistory
            .Where(item => item.CapturedAtUtc >= superDealWindowStart)
            .Select(item => (decimal?)item.Price)
            .Min();

        var historyEntry = new ProductPriceHistoryEntry
        {
            SourceEventId = scrapedEvent.EventId,
            ProductId = scrapedEvent.ProductId,
            CategoryId = scrapedEvent.CategoryId,
            CategoryName = scrapedEvent.CategoryName,
            GuildId = scrapedEvent.GuildId,
            ProductName = scrapedEvent.ProductName,
            ProductUrl = scrapedEvent.ProductUrl,
            SiteName = scrapedEvent.SiteName,
            BusinessType = scrapedEvent.BusinessType,
            Price = scrapedEvent.CurrentPrice,
            DiscountPercentage = scrapedEvent.DiscountPercentage,
            Currency = scrapedEvent.Currency,
            CapturedAtUtc = scrapedEvent.ScrapedAtUtc
        };

        _dbContext.ProductPriceHistory.Add(historyEntry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var isRecentWindowDeal = recentWindowLowestPrice.HasValue
            ? scrapedEvent.CurrentPrice < recentWindowLowestPrice.Value
            : previousPrice.HasValue && scrapedEvent.CurrentPrice < previousPrice.Value;

        if (!isRecentWindowDeal)
        {
            return null;
        }

        var isSuperDeal = historicalLowestPrice.HasValue && scrapedEvent.CurrentPrice <= historicalLowestPrice.Value;

        return new ProductDealDetectedEvent
        {
            EventId = Guid.NewGuid(),
            CorrelationId = scrapedEvent.CorrelationId,
            ProductId = scrapedEvent.ProductId,
            CategoryId = scrapedEvent.CategoryId,
            CategoryName = scrapedEvent.CategoryName,
            GuildId = scrapedEvent.GuildId,
            ProductName = scrapedEvent.ProductName,
            ProductUrl = scrapedEvent.ProductUrl,
            SiteName = scrapedEvent.SiteName,
            BusinessType = scrapedEvent.BusinessType,
            CurrentPrice = scrapedEvent.CurrentPrice,
            DiscountPercentage = scrapedEvent.DiscountPercentage,
            PreviousPrice = previousPrice,
            RecentWindowLowestPrice = recentWindowLowestPrice,
            HistoricalLowestPrice = historicalLowestPrice,
            RecentWindowDays = options.RecentWindowDays,
            HistoricalWindowDays = options.SuperDealWindowDays,
            Currency = scrapedEvent.Currency,
            DealKind = isSuperDeal ? DealKind.SuperDeal : DealKind.Deal,
            OccurredAtUtc = scrapedEvent.ScrapedAtUtc
        };
    }
}
