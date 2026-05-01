using Microsoft.EntityFrameworkCore;
using ScrapWars.Application.DTO;
using ScrapWars.Application.Interfaces;
using ScrapWars.Infrastructure.Persistence;

namespace ScrapWars.Infrastructure.Services;

public class ProductPriceHistoryService : IProductPriceHistoryService
{
    private readonly ProductPriceHistoryReadDbContext _dbContext;

    public ProductPriceHistoryService(ProductPriceHistoryReadDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyDictionary<Guid, ProductPriceSnapshot>> GetLatestSnapshotsAsync(
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        var productIdSet = productIds.Distinct().ToArray();

        if (productIdSet.Length == 0)
        {
            return new Dictionary<Guid, ProductPriceSnapshot>();
        }

        var rows = await _dbContext.ProductPriceHistory
            .AsNoTracking()
            .Where(item => productIdSet.Contains(item.ProductId))
            .OrderByDescending(item => item.CapturedAtUtc)
            .ToArrayAsync(cancellationToken);

        return rows
            .GroupBy(item => item.ProductId)
            .ToDictionary(group => group.Key, group => Map(group.First()));
    }

    public async Task<IReadOnlyDictionary<Guid, ProductPriceSnapshot>> WaitForUpdatedSnapshotsAsync(
        IEnumerable<Guid> productIds,
        DateTime commandStartedAtUtc,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var productIdSet = productIds.Distinct().ToArray();

        if (productIdSet.Length == 0)
        {
            return new Dictionary<Guid, ProductPriceSnapshot>();
        }

        var deadlineUtc = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadlineUtc && !cancellationToken.IsCancellationRequested)
        {
            var snapshots = await _dbContext.ProductPriceHistory
                .AsNoTracking()
                .Where(item =>
                    productIdSet.Contains(item.ProductId) &&
                    item.CapturedAtUtc >= commandStartedAtUtc)
                .OrderByDescending(item => item.CapturedAtUtc)
                .ToArrayAsync(cancellationToken);

            var latestByProduct = snapshots
                .GroupBy(item => item.ProductId)
                .ToDictionary(group => group.Key, group => Map(group.First()));

            if (productIdSet.All(productId => latestByProduct.ContainsKey(productId)))
            {
                return latestByProduct;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        return await GetLatestSnapshotsAsync(productIdSet, cancellationToken);
    }

    private static ProductPriceSnapshot Map(ProductPriceHistoryReadModel item)
    {
        return new ProductPriceSnapshot
        {
            ProductId = item.ProductId,
            Price = item.Price,
            DiscountPercentage = item.DiscountPercentage,
            Currency = item.Currency,
            CapturedAtUtc = item.CapturedAtUtc
        };
    }
}
