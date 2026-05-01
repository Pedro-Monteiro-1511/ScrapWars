using ScrapWars.Application.DTO;
using ScrapWars.Domain.Entities;

namespace ScrapWars.Application.Interfaces;

public interface IGuildSubscriptionService
{
    Task<GuildSubscription> GetOrCreateSubscriptionAsync(ulong guildId);
    Task<GuildSubscription> SaveSubscriptionAsync(
        ulong guildId,
        GuildSubscriptionPlan plan,
        int extraChannels,
        int extraCategories,
        int extraProducts,
        decimal extraChannelUnitPrice,
        decimal extraCategoryUnitPrice,
        decimal extraProductUnitPrice,
        string currencyCode);
    Task<GuildEntitlementSummary> GetEntitlementSummaryAsync(ulong guildId);
    Task EnsureCategoryCapacityAsync(ulong guildId);
    Task EnsureNotificationChannelCapacityAsync(ulong guildId, ulong channelId);
    Task EnsureProductCapacityAsync(ulong guildId);
}
