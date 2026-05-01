using Microsoft.EntityFrameworkCore;
using ScrapWars.Application.Interfaces;
using ScrapWars.Domain.Entities;
using ScrapWars.Infrastructure.Persistence;

namespace ScrapWars.Infrastructure.Services;

public class GuildConfigurationService : IGuildConfigurationService
{
    private readonly ScrapWarsDbContext _dbContext;
    private readonly IGuildSubscriptionService _guildSubscriptionService;

    public GuildConfigurationService(
        ScrapWarsDbContext dbContext,
        IGuildSubscriptionService guildSubscriptionService)
    {
        _dbContext = dbContext;
        _guildSubscriptionService = guildSubscriptionService;
    }

    public async Task<ProductCategory> CreateCategoryAsync(string name, ulong guildId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var trimmedName = name.Trim();
        var normalizedName = ProductCategory.NormalizeName(trimmedName);
        var exists = await _dbContext.ProductCategories.AnyAsync(category =>
            category.GuildId == guildId &&
            category.NormalizedName == normalizedName);

        if (exists)
        {
            throw new InvalidOperationException($"Category '{trimmedName}' already exists in this server.");
        }

        await _guildSubscriptionService.EnsureCategoryCapacityAsync(guildId);

        var category = new ProductCategory(trimmedName, guildId);
        _dbContext.ProductCategories.Add(category);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, "UX_product_categories_guild_id_normalized_name"))
        {
            throw new InvalidOperationException($"Category '{trimmedName}' already exists in this server.", ex);
        }

        return category;
    }

    public async Task<IReadOnlyCollection<ProductCategory>> GetCategoriesAsync(ulong guildId)
    {
        return await _dbContext.ProductCategories
            .AsNoTracking()
            .Where(category => category.GuildId == guildId)
            .OrderBy(category => category.Name)
            .ToArrayAsync();
    }

    public async Task<bool> DeleteCategoryAsync(string name, ulong guildId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var category = await FindCategoryAsync(name, guildId);

        if (category is null)
        {
            return false;
        }

        var hasProducts = await _dbContext.Products.AnyAsync(product => product.CategoryId == category.Id);

        if (hasProducts)
        {
            throw new InvalidOperationException($"Category '{category.Name}' still has products. Move or delete those products first.");
        }

        _dbContext.ProductCategories.Remove(category);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<CategoryNotificationChannel> AddNotificationChannelAsync(string categoryName, ulong channelId, ulong guildId)
    {
        var category = await FindCategoryAsync(categoryName, guildId)
            ?? throw new InvalidOperationException($"Category '{categoryName.Trim()}' does not exist in this server.");

        var exists = await _dbContext.CategoryNotificationChannels.AnyAsync(channel =>
            channel.CategoryId == category.Id &&
            channel.ChannelId == channelId);

        if (exists)
        {
            throw new InvalidOperationException($"Channel {channelId} is already configured for category '{category.Name}'.");
        }

        await _guildSubscriptionService.EnsureNotificationChannelCapacityAsync(guildId, channelId);

        var notificationChannel = new CategoryNotificationChannel(guildId, category.Id, channelId);
        _dbContext.CategoryNotificationChannels.Add(notificationChannel);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, "UX_category_notification_channels_category_id_channel_id"))
        {
            throw new InvalidOperationException($"Channel {channelId} is already configured for category '{category.Name}'.", ex);
        }

        notificationChannel.Category = category;

        return notificationChannel;
    }

    public async Task<bool> RemoveNotificationChannelAsync(string categoryName, ulong channelId, ulong guildId)
    {
        var category = await FindCategoryAsync(categoryName, guildId);

        if (category is null)
        {
            return false;
        }

        var notificationChannel = await _dbContext.CategoryNotificationChannels.FirstOrDefaultAsync(channel =>
            channel.CategoryId == category.Id &&
            channel.ChannelId == channelId);

        if (notificationChannel is null)
        {
            return false;
        }

        _dbContext.CategoryNotificationChannels.Remove(notificationChannel);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<IReadOnlyCollection<CategoryNotificationChannel>> GetNotificationChannelsAsync(string categoryName, ulong guildId)
    {
        var category = await FindCategoryAsync(categoryName, guildId);

        if (category is null)
        {
            return [];
        }

        return await _dbContext.CategoryNotificationChannels
            .AsNoTracking()
            .Include(channel => channel.Category)
            .Where(channel => channel.CategoryId == category.Id)
            .OrderBy(channel => channel.ChannelId)
            .ToArrayAsync();
    }

    private Task<ProductCategory?> FindCategoryAsync(string name, ulong guildId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalizedName = ProductCategory.NormalizeName(name);

        return _dbContext.ProductCategories.FirstOrDefaultAsync(category =>
            category.GuildId == guildId &&
            category.NormalizedName == normalizedName);
    }

    private static bool IsUniqueViolation(DbUpdateException exception, string constraintName)
    {
        return exception.InnerException?.Message.Contains(constraintName, StringComparison.Ordinal) == true;
    }
}
