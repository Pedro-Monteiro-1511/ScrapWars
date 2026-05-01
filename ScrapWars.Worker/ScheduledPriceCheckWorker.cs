using Microsoft.Extensions.Options;
using ScrapWars.Application.Interfaces;
using ScrapWars.Infrastructure.Services;

namespace ScrapWars.Worker;

public class ScheduledPriceCheckWorker : BackgroundService
{
    private static readonly TimeOnly MorningRun = new(8, 0);
    private static readonly TimeOnly EveningRun = new(20, 0);

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOptions<ScheduledPriceCheckOptions> _options;
    private readonly ILogger<ScheduledPriceCheckWorker> _logger;

    public ScheduledPriceCheckWorker(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<ScheduledPriceCheckOptions> options,
        ILogger<ScheduledPriceCheckWorker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Value.Enabled)
        {
            _logger.LogInformation("Scheduled price checks are disabled.");
            return;
        }

        var timeZone = ResolveTimeZone(_options.Value.TimeZoneId);

        while (!stoppingToken.IsCancellationRequested)
        {
            var nowUtc = DateTime.UtcNow;
            var nextRunUtc = GetNextRunUtc(nowUtc, timeZone);
            var delay = nextRunUtc - nowUtc;

            _logger.LogInformation(
                "Next scheduled price check run at {NextRunUtc} ({TimeZoneId}).",
                nextRunUtc,
                timeZone.Id);

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, stoppingToken);
            }

            await RunScheduledChecksAsync(stoppingToken);
        }
    }

    private async Task RunScheduledChecksAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
        var historyService = scope.ServiceProvider.GetRequiredService<IProductPriceHistoryService>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPriceCheckRequestPublisher>();

        var products = await productService.GetAllProductsAsync();

        if (products.Count == 0)
        {
            _logger.LogInformation("Scheduled price check run skipped because there are no registered products.");
            return;
        }

        var latestSnapshots = await historyService.GetLatestSnapshotsAsync(products.Select(product => product.Id), cancellationToken);

        var queued = 0;
        var failed = 0;

        foreach (var product in products)
        {
            try
            {
                latestSnapshots.TryGetValue(product.Id, out var snapshot);
                await publisher.PublishAsync(product, snapshot?.Price, cancellationToken);
                queued++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogWarning(ex, "Failed to queue scheduled price check for product {ProductId} ({ProductName}).", product.Id, product.Name);
            }
        }

        _logger.LogInformation(
            "Scheduled price check run finished. Queued {QueuedCount} products, failed {FailedCount}.",
            queued,
            failed);
    }

    private static TimeZoneInfo ResolveTimeZone(string configuredTimeZoneId)
    {
        var candidates = new[]
        {
            configuredTimeZoneId,
            "Europe/Lisbon",
            "GMT Standard Time"
        };

        foreach (var candidate in candidates.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(candidate);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Local;
    }

    private static DateTime GetNextRunUtc(DateTime nowUtc, TimeZoneInfo timeZone)
    {
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone);
        var localDate = DateOnly.FromDateTime(localNow);

        var candidates = new[]
        {
            localDate.ToDateTime(MorningRun),
            localDate.ToDateTime(EveningRun),
            localDate.AddDays(1).ToDateTime(MorningRun)
        };

        var nextLocal = candidates.First(candidate => candidate > localNow);
        return TimeZoneInfo.ConvertTimeToUtc(nextLocal, timeZone);
    }
}
