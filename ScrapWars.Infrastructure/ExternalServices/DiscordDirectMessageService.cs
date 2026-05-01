using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using ScrapWars.Application.Interfaces;

namespace ScrapWars.Infrastructure.ExternalServices;

public class DiscordDirectMessageService : IDirectMessageService
{
    private readonly HttpClient _httpClient;
    private readonly string _botToken;

    public DiscordDirectMessageService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://discord.com/api/v10/");

        _botToken = configuration["Discord:Token"]
            ?? throw new InvalidOperationException("Discord token is not configured.");
    }

    public async Task SendHelpMessageAsync(ulong userId, CancellationToken cancellationToken = default)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _botToken);

        var dmChannel = await CreateDmChannelAsync(userId, cancellationToken);
        await SendMessageAsync(dmChannel.Id, BuildHelpMessage(), cancellationToken);
    }

    private async Task<DiscordChannelResponse> CreateDmChannelAsync(ulong userId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "users/@me/channels",
            new CreateDmChannelRequest(userId.ToString()),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<DiscordChannelResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Discord did not return a DM channel.");
    }

    private async Task SendMessageAsync(string channelId, string content, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            $"channels/{channelId}/messages",
            new CreateMessageRequest(content),
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private static string BuildHelpMessage()
    {
        return
            """
            ScrapWars BOT

            O que podes fazer:
            /help
            Recebes esta documentacao por mensagem privada.

            /category-create
            Cria uma categoria para este servidor.

            /category-list
            Lista as categorias configuradas no servidor.

            /category-delete
            Remove uma categoria vazia.

            /category-channel-add
            Liga uma categoria a um canal de notificacoes.

            /category-channel-remove
            Remove a ligacao entre categoria e canal.

            /category-channel-list
            Lista os canais configurados para uma categoria.

            /product-add
            Adiciona um produto e associa-o a uma categoria do servidor.

            /product-list
            Lista os produtos do servidor.

            /product-delete
            Remove um produto pelo nome.
            """;
    }

    private sealed record CreateDmChannelRequest(string recipient_id);

    private sealed class DiscordChannelResponse
    {
        public string Id { get; init; } = string.Empty;
    }

    private sealed record CreateMessageRequest(string content);
}
