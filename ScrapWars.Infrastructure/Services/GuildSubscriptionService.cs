using Microsoft.EntityFrameworkCore;
using ScrapWars.Application.DTO;
using ScrapWars.Application.Interfaces;
using ScrapWars.Domain.Entities;
using ScrapWars.Infrastructure.Persistence;

namespace ScrapWars.Infrastructure.Services;

public class GuildSubscriptionService : IGuildSubscriptionService
{
    private const string GuildSubscriptionGuildIdConstraint = "UX_guild_subscriptions_guild_id";

    private readonly ScrapWarsDbContext _dbContext;

    public GuildSubscriptionService(ScrapWarsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GuildSubscription> GetOrCreateSubscriptionAsync(ulong guildId)
    {
        var subscription = await _dbContext.GuildSubscriptions.FirstOrDefaultAsync(item => item.GuildId == guildId);

        if (subscription is not null)
        {
            return subscription;
        }

        subscription = new GuildSubscription(guildId);
        _dbContext.GuildSubscriptions.Add(subscription);

        try
        {
            await _dbContext.SaveChangesAsync();
            return subscription;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, GuildSubscriptionGuildIdConstraint))
        {
            return await _dbContext.GuildSubscriptions.FirstAsync(item => item.GuildId == guildId);
        }
    }

    public async Task<GuildSubscription> SaveSubscriptionAsync(
        ulong guildId,
        GuildSubscriptionPlan plan,
        int extraChannels,
        int extraCategories,
        int extraProducts,
        decimal extraChannelUnitPrice,
        decimal extraCategoryUnitPrice,
        decimal extraProductUnitPrice,
        string currencyCode)
    {
        var subscription = await GetOrCreateSubscriptionAsync(guildId);

        subscription.Configure(
            plan,
            extraChannels,
            extraCategories,
            extraProducts,
            extraChannelUnitPrice,
            extraCategoryUnitPrice,
            extraProductUnitPrice,
            currencyCode);

        await _dbContext.SaveChangesAsync();

        return subscription;
    }

    public async Task<GuildEntitlementSummary> GetEntitlementSummaryAsync(ulong guildId)
    {
        var subscription = await GetOrCreateSubscriptionAsync(guildId);
        var definition = GuildPlanCatalog.GetDefinition(subscription.Plan);

        var usedChannels = await _dbContext.CategoryNotificationChannels
            .Where(channel => channel.GuildId == guildId)
            .Select(channel => channel.ChannelId)
            .Distinct()
            .CountAsync();

        var usedCategories = await _dbContext.ProductCategories.CountAsync(category => category.GuildId == guildId);
        var usedProducts = await _dbContext.Products.CountAsync(product => product.GuildId == guildId);

        return new GuildEntitlementSummary
        {
            GuildId = guildId,
            Plan = subscription.Plan,
            PlanDisplayName = definition.DisplayName,
            IncludedChannels = definition.ChannelsIncluded,
            IncludedCategories = definition.CategoriesIncluded,
            IncludedProducts = definition.ProductsIncluded,
            ExtraChannels = subscription.ExtraChannels,
            ExtraCategories = subscription.ExtraCategories,
            ExtraProducts = subscription.ExtraProducts,
            UsedChannels = usedChannels,
            UsedCategories = usedCategories,
            UsedProducts = usedProducts,
            ExtraChannelUnitPrice = subscription.ExtraChannelUnitPrice,
            ExtraCategoryUnitPrice = subscription.ExtraCategoryUnitPrice,
            ExtraProductUnitPrice = subscription.ExtraProductUnitPrice,
            CurrencyCode = subscription.CurrencyCode
        };
    }

    public async Task EnsureCategoryCapacityAsync(ulong guildId)
    {
        var summary = await GetEntitlementSummaryAsync(guildId);

        if (summary.UsedCategories < summary.TotalCategories)
        {
            return;
        }

        throw new InvalidOperationException(
            $"This server has reached the category limit for the {summary.PlanDisplayName} plan ({summary.TotalCategories}). Add category capacity or upgrade the plan.");
    }

    public async Task EnsureNotificationChannelCapacityAsync(ulong guildId, ulong channelId)
    {
        var channelAlreadyConfigured = await _dbContext.CategoryNotificationChannels.AnyAsync(channel =>
            channel.GuildId == guildId &&
            channel.ChannelId == channelId);

        if (channelAlreadyConfigured)
        {
            return;
        }

        var summary = await GetEntitlementSummaryAsync(guildId);

        if (summary.UsedChannels < summary.TotalChannels)
        {
            return;
        }

        throw new InvalidOperationException(
            $"This server has reached the notification channel limit for the {summary.PlanDisplayName} plan ({summary.TotalChannels}). Add channel capacity or upgrade the plan.");
    }

    public async Task EnsureProductCapacityAsync(ulong guildId)
    {
        var summary = await GetEntitlementSummaryAsync(guildId);

        if (summary.UsedProducts < summary.TotalProducts)
        {
            return;
        }

        throw new InvalidOperationException(
            $"This server has reached the product limit for the {summary.PlanDisplayName} plan ({summary.TotalProducts}). Add product capacity or upgrade the plan.");
    }

    private static bool IsUniqueViolation(DbUpdateException exception, string constraintName)
    {
        return exception.InnerException?.Message.Contains(constraintName, StringComparison.Ordinal) == true;
    }
}
