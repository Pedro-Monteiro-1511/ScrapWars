using NetCord;
using NetCord.Services.ApplicationCommands;

namespace ScrapWars.Infrastructure.Commands;

public class ChannelModule : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("channel-register", "Register a channel")]
    public string RegisterChannel(
    [SlashCommandParameter(Name = "channel", Description = "Select a text channel")] Channel channel)
    {
        return $"Configured channel {channel.Id} for SuperDeals notifications.";
    }


}
