using ScrapWars.Application.DTO;

namespace ScrapWars.Application.Interfaces;

public interface IProductPriceHistoryService
{
    Task<IReadOnlyDictionary<Guid, ProductPriceSnapshot>> GetLatestSnapshotsAsync(
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, ProductPriceSnapshot>> WaitForUpdatedSnapshotsAsync(
        IEnumerable<Guid> productIds,
        DateTime commandStartedAtUtc,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
