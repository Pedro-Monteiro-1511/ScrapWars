using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ScrapWars.Notifications.Worker.Discord;

public class DiscordChannelNotifier : IDiscordChannelNotifier
{
    private readonly HttpClient _httpClient;
    private readonly string _botToken;

    public DiscordChannelNotifier(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://discord.com/api/v10/");

        _botToken = configuration["Discord:Token"]
            ?? throw new InvalidOperationException("Discord:Token is not configured for the notifications worker.");
    }

    public async Task SendMessageAsync(ulong channelId, string content, CancellationToken cancellationToken = default)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _botToken);

        using var response = await _httpClient.PostAsJsonAsync(
            $"channels/{channelId}/messages",
            new CreateMessageRequest(content),
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private sealed record CreateMessageRequest(string content);
}
