using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using ScrapWars.Application.Interfaces;

namespace ScrapWars.Infrastructure.ExternalServices;

public class BotService : IBotService
{
    private GatewayClient? _client;
    private readonly ILogger<BotService> _logger;
    private readonly ApplicationCommandService<ApplicationCommandContext> _commandService;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public BotService(
        ILogger<BotService> logger,
        ApplicationCommandService<ApplicationCommandContext> commandService,
        IConfiguration configuration,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _commandService = commandService;
        _configuration = configuration;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task StartAsync(string token, CancellationToken cancellationToken)
    {
        try
        {
            _client = new GatewayClient(new BotToken(token));

            _commandService.AddModules(typeof(BotService).Assembly);

            _client.Ready += async (ReadyEventArgs args) =>
            {
                _logger.LogInformation($"Bot pronto: {args.User.Username}");

                var restClient = new RestClient(new BotToken(token));
                var guildId = GetConfiguredGuildId();

                await _commandService.RegisterCommandsAsync(restClient, _client.Id, guildId: guildId);

                _logger.LogInformation(
                    guildId.HasValue
                        ? "Slash commands registados no servidor {GuildId}."
                        : "Slash commands globais registados.",
                    guildId);
            };

            _client.InteractionCreate += async (interaction) =>
            {
                if (interaction is not ApplicationCommandInteraction cmd)
                    return;

                var context = new ApplicationCommandContext(cmd, _client);
                using var scope = _serviceScopeFactory.CreateScope();

                try
                {
                    await _commandService.ExecuteAsync(context, scope.ServiceProvider);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao executar o comando '{CommandName}'.", cmd.Data.Name);

                    await cmd.SendResponseAsync(InteractionCallback.Message(
                        new InteractionMessageProperties
                        {
                            Content = "Ocorreu um erro a processar este comando.",
                            Flags = MessageFlags.Ephemeral
                        }));
                }
            };

            _logger.LogInformation("Iniciando Discord Bot...");
            await _client.StartAsync();

            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao iniciar Discord Bot");
            throw;
        }
    }

    public async Task StopAsync()
    {
        if (_client != null)
        {
            await _client.CloseAsync();
            _client.Dispose();
        }
    }

    private ulong? GetConfiguredGuildId()
    {
        var configuredGuildId = _configuration["Discord:GuildId"];

        if (string.IsNullOrWhiteSpace(configuredGuildId))
        {
            return null;
        }

        return ulong.TryParse(configuredGuildId, out var guildId)
            ? guildId
            : throw new InvalidOperationException("Discord:GuildId must be a valid Discord server ID.");
    }
}
