using ScrapWars.Domain.Entities;

namespace ScrapWars.Application.Interfaces;

public interface IGuildConfigurationService
{
    Task<ProductCategory> CreateCategoryAsync(string name, ulong guildId);
    Task<IReadOnlyCollection<ProductCategory>> GetCategoriesAsync(ulong guildId);
    Task<bool> DeleteCategoryAsync(string name, ulong guildId);
    Task<CategoryNotificationChannel> AddNotificationChannelAsync(string categoryName, ulong channelId, ulong guildId);
    Task<bool> RemoveNotificationChannelAsync(string categoryName, ulong channelId, ulong guildId);
    Task<IReadOnlyCollection<CategoryNotificationChannel>> GetNotificationChannelsAsync(string categoryName, ulong guildId);
}
