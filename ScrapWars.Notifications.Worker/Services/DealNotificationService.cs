using Microsoft.EntityFrameworkCore;
using ScrapWars.Contracts.Events;
using ScrapWars.Notifications.Worker.Data;
using ScrapWars.Notifications.Worker.Discord;

namespace ScrapWars.Notifications.Worker.Services;

public class DealNotificationService
{
    private readonly NotificationRoutingDbContext _dbContext;
    private readonly IDiscordChannelNotifier _discordChannelNotifier;
    private readonly ILogger<DealNotificationService> _logger;

    public DealNotificationService(
        NotificationRoutingDbContext dbContext,
        IDiscordChannelNotifier discordChannelNotifier,
        ILogger<DealNotificationService> logger)
    {
        _dbContext = dbContext;
        _discordChannelNotifier = discordChannelNotifier;
        _logger = logger;
    }

    public async Task NotifyAsync(ProductDealDetectedEvent dealEvent, CancellationToken cancellationToken)
    {
        if (!dealEvent.CategoryId.HasValue)
        {
            _logger.LogInformation(
                "Skipping deal notification for product {ProductId} because there is no category to route channels.",
                dealEvent.ProductId);
            return;
        }

        var channels = await _dbContext.CategoryNotificationChannels
            .AsNoTracking()
            .Where(item => item.GuildId == dealEvent.GuildId && item.CategoryId == dealEvent.CategoryId.Value)
            .Select(item => item.ChannelId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        if (channels.Length == 0)
        {
            _logger.LogInformation(
                "No notification channels configured for guild {GuildId} and category {CategoryId}.",
                dealEvent.GuildId,
                dealEvent.CategoryId.Value);
            return;
        }

        var message = BuildMessage(dealEvent);

        foreach (var channelId in channels)
        {
            await _discordChannelNotifier.SendMessageAsync(channelId, message, cancellationToken);
        }
    }

    private static string BuildMessage(ProductDealDetectedEvent dealEvent)
    {
        var label = dealEvent.DealKind == DealKind.SuperDeal ? "SUPER DEAL" : "DEAL";
        var previousPriceText = dealEvent.PreviousPrice.HasValue
            ? $" | Antes: {dealEvent.PreviousPrice.Value:F2} {dealEvent.Currency}"
            : string.Empty;
        var discountText = dealEvent.DiscountPercentage.HasValue
            ? $" | Desconto: {dealEvent.DiscountPercentage.Value:F2}%"
            : string.Empty;

        return
            $"{label} | {dealEvent.ProductName}{Environment.NewLine}" +
            $"Negocio: {GetBusinessTypeLabel(dealEvent.BusinessType)}{Environment.NewLine}" +
            $"Categoria: {dealEvent.CategoryName}{Environment.NewLine}" +
            $"Preco atual: {dealEvent.CurrentPrice:F2} {dealEvent.Currency}{previousPriceText}{discountText}{Environment.NewLine}" +
            $"Site: {dealEvent.SiteName}{Environment.NewLine}" +
            $"{dealEvent.ProductUrl}";
    }

    private static string GetBusinessTypeLabel(ListingBusinessType businessType)
    {
        return businessType switch
        {
            ListingBusinessType.Sale => "Venda",
            ListingBusinessType.Rent => "Arrendamento",
            _ => "Desconhecido"
        };
    }
}
