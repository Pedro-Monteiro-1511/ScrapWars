namespace ScrapWars.Notifications.Worker.Discord;

public interface IDiscordChannelNotifier
{
    Task SendMessageAsync(ulong channelId, string content, CancellationToken cancellationToken = default);
}
