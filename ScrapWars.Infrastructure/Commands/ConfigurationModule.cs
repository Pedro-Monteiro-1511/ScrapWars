using NetCord;
using NetCord.Services.ApplicationCommands;
using ScrapWars.Application.Interfaces;

namespace ScrapWars.Infrastructure.Commands;

public class ConfigurationModule : ApplicationCommandModule<ApplicationCommandContext>
{
    private readonly IGuildConfigurationService _guildConfigurationService;

    public ConfigurationModule(IGuildConfigurationService guildConfigurationService)
    {
        _guildConfigurationService = guildConfigurationService;
    }

    [SlashCommand("category-create", "Create a product category for this server")]
    public async Task<string> CreateCategoryAsync(
        [SlashCommandParameter(Name = "name", Description = "Category name")] string name)
    {
        if (!TryGetGuildId(out var guildId, out var error))
        {
            return error;
        }

        try
        {
            var category = await _guildConfigurationService.CreateCategoryAsync(name, guildId);

            return $"Category created: {category.Name}";
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message;
        }
    }

    [SlashCommand("category-list", "List product categories for this server")]
    public async Task<string> ListCategoriesAsync()
    {
        if (!TryGetGuildId(out var guildId, out var error))
        {
            return error;
        }

        var categories = await _guildConfigurationService.GetCategoriesAsync(guildId);

        if (categories.Count == 0)
        {
            return "No categories configured yet.";
        }

        return $"Configured categories:{Environment.NewLine}{string.Join(Environment.NewLine, categories.Select(category => $"- {category.Name}"))}";
    }

    [SlashCommand("category-delete", "Delete an empty product category from this server")]
    public async Task<string> DeleteCategoryAsync(
        [SlashCommandParameter(Name = "name", Description = "Category name")] string name)
    {
        if (!TryGetGuildId(out var guildId, out var error))
        {
            return error;
        }

        try
        {
            var deleted = await _guildConfigurationService.DeleteCategoryAsync(name, guildId);

            return deleted
                ? $"Category deleted: {name.Trim()}"
                : $"Category not found: {name.Trim()}";
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message;
        }
    }

    [SlashCommand("category-channel-add", "Send notifications for a category to a channel")]
    public async Task<string> AddCategoryChannelAsync(
        [SlashCommandParameter(Name = "category", Description = "Category name")] string category,
        [SlashCommandParameter(Name = "channel", Description = "Notification channel")] Channel channel)
    {
        if (!TryGetGuildId(out var guildId, out var error))
        {
            return error;
        }

        try
        {
            await _guildConfigurationService.AddNotificationChannelAsync(category, channel.Id, guildId);

            return $"Channel <#{channel.Id}> will receive notifications for category '{category.Trim()}'.";
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message;
        }
    }

    [SlashCommand("category-channel-remove", "Stop sending category notifications to a channel")]
    public async Task<string> RemoveCategoryChannelAsync(
        [SlashCommandParameter(Name = "category", Description = "Category name")] string category,
        [SlashCommandParameter(Name = "channel", Description = "Notification channel")] Channel channel)
    {
        if (!TryGetGuildId(out var guildId, out var error))
        {
            return error;
        }

        var removed = await _guildConfigurationService.RemoveNotificationChannelAsync(category, channel.Id, guildId);

        return removed
            ? $"Channel <#{channel.Id}> removed from category '{category.Trim()}'."
            : $"Channel <#{channel.Id}> was not configured for category '{category.Trim()}'.";
    }

    [SlashCommand("category-channel-list", "List notification channels for a category")]
    public async Task<string> ListCategoryChannelsAsync(
        [SlashCommandParameter(Name = "category", Description = "Category name")] string category)
    {
        if (!TryGetGuildId(out var guildId, out var error))
        {
            return error;
        }

        var channels = await _guildConfigurationService.GetNotificationChannelsAsync(category, guildId);

        if (channels.Count == 0)
        {
            return $"No notification channels configured for category '{category.Trim()}'.";
        }

        var channelLines = channels.Select(channel => $"- <#{channel.ChannelId}>");

        return $"Notification channels for '{category.Trim()}':{Environment.NewLine}{string.Join(Environment.NewLine, channelLines)}";
    }

    private bool TryGetGuildId(out ulong guildId, out string error)
    {
        guildId = Context.Interaction.GuildId ?? 0;
        error = string.Empty;

        if (guildId == 0)
        {
            error = "This command must be used inside a Discord server.";
            return false;
        }

        return true;
    }
}
