using ScrapWars.Application.Interfaces;

namespace ScrapWars.Worker;

public class Worker : BackgroundService
{
    private readonly IBotService _discordBotService;
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;

    public Worker(
        IBotService discordBotService,
        ILogger<Worker> logger,
        IConfiguration configuration)
    {
        _discordBotService = discordBotService;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var token = _configuration["Discord:Token"]
                ?? throw new InvalidOperationException("Discord token não configurado");

            _logger.LogInformation("A iniciar Discord Bot...");

            await _discordBotService.StartAsync(token, stoppingToken);

            _logger.LogInformation("Discord Bot em execução");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Worker cancelado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro fatal no Worker");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("A parar Discord Bot...");
        await _discordBotService.StopAsync();
        await base.StopAsync(cancellationToken);
    }
}