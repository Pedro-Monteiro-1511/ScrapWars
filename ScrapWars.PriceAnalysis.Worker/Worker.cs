using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ScrapWars.Contracts.Events;
using ScrapWars.PriceAnalysis.Worker.Messaging;
using ScrapWars.PriceAnalysis.Worker.Persistence;
using ScrapWars.PriceAnalysis.Worker.Services;

namespace ScrapWars.PriceAnalysis.Worker;

public class PriceAnalysisWorker : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<PriceAnalysisWorker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOptions<RabbitMqOptions> _rabbitMqOptions;
    private readonly IOptions<RabbitMqTopologyOptions> _topologyOptions;

    public PriceAnalysisWorker(
        ILogger<PriceAnalysisWorker> logger,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IOptions<RabbitMqTopologyOptions> topologyOptions)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _rabbitMqOptions = rabbitMqOptions;
        _topologyOptions = topologyOptions;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitMq = _rabbitMqOptions.Value;
        var topology = _topologyOptions.Value;

        ValidateConfiguration(rabbitMq, topology);

        using var startupScope = _serviceScopeFactory.CreateScope();
        var dbContext = startupScope.ServiceProvider.GetRequiredService<PriceHistoryDbContext>();
        await dbContext.Database.EnsureCreatedAsync(stoppingToken);

        var connectionFactory = new ConnectionFactory
        {
            HostName = rabbitMq.HostName,
            Port = rabbitMq.Port,
            UserName = rabbitMq.UserName,
            Password = rabbitMq.Password,
            VirtualHost = rabbitMq.VirtualHost,
            DispatchConsumersAsync = true
        };

        using var connection = await CreateConnectionWithRetryAsync(connectionFactory, stoppingToken);
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(topology.PriceScrapedExchange, ExchangeType.Fanout, durable: true, autoDelete: false);
        channel.ExchangeDeclare(topology.DealDetectedExchange, ExchangeType.Fanout, durable: true, autoDelete: false);
        channel.ExchangeDeclare(topology.DealDetectionFailedExchange, ExchangeType.Fanout, durable: true, autoDelete: false);
        channel.QueueDeclare(topology.PriceScrapedQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(topology.PriceScrapedQueue, topology.PriceScrapedExchange, routingKey: string.Empty);
        channel.BasicQos(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, eventArgs) => await HandleMessageAsync(channel, eventArgs, topology, stoppingToken);

        var consumerTag = channel.BasicConsume(topology.PriceScrapedQueue, autoAck: false, consumer: consumer);

        _logger.LogInformation(
            "Price analysis worker listening on queue '{Queue}' bound to exchange '{Exchange}'.",
            topology.PriceScrapedQueue,
            topology.PriceScrapedExchange);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        finally
        {
            if (channel.IsOpen)
            {
                channel.BasicCancel(consumerTag);
            }
        }
    }

    private async Task<IConnection> CreateConnectionWithRetryAsync(
        ConnectionFactory connectionFactory,
        CancellationToken cancellationToken)
    {
        const int delaySeconds = 5;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                return connectionFactory.CreateConnection();
            }
            catch (BrokerUnreachableException ex)
            {
                _logger.LogWarning(
                    ex,
                    "RabbitMQ is not reachable for the price analysis worker yet. Retrying in {DelaySeconds} seconds...",
                    delaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }
        }

        throw new OperationCanceledException(cancellationToken);
    }

    private async Task HandleMessageAsync(
        IModel channel,
        BasicDeliverEventArgs eventArgs,
        RabbitMqTopologyOptions topology,
        CancellationToken cancellationToken)
    {
        ProductPriceScrapedEvent? message = null;

        try
        {
            message = JsonSerializer.Deserialize<ProductPriceScrapedEvent>(
                Encoding.UTF8.GetString(eventArgs.Body.ToArray()),
                SerializerOptions);

            if (message is null)
            {
                _logger.LogWarning("Received an empty price scraped payload.");
                channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                return;
            }

            using var scope = _serviceScopeFactory.CreateScope();
            var analysisService = scope.ServiceProvider.GetRequiredService<PriceHistoryAnalysisService>();
            var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            var dealEvent = await analysisService.RecordAndAnalyzeAsync(message, cancellationToken);

            if (dealEvent is not null)
            {
                await eventPublisher.PublishAsync(
                    topology.DealDetectedExchange,
                    topology.DealDetectedRoutingKey,
                    dealEvent,
                    cancellationToken);
            }

            channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing a scraped price event.");

            if (message is not null)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

                var failureEvent = new ProductDealDetectionFailedEvent
                {
                    EventId = Guid.NewGuid(),
                    CorrelationId = message.CorrelationId,
                    ProductId = message.ProductId,
                    CategoryId = message.CategoryId,
                    CategoryName = message.CategoryName,
                    GuildId = message.GuildId,
                    ProductName = message.ProductName,
                    ProductUrl = message.ProductUrl,
                    FailureReason = ex.Message,
                    OccurredAtUtc = DateTime.UtcNow
                };

                await eventPublisher.PublishAsync(
                    topology.DealDetectionFailedExchange,
                    topology.DealDetectionFailedRoutingKey,
                    failureEvent,
                    cancellationToken);

                channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                return;
            }

            channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
        }
    }

    private static void ValidateConfiguration(RabbitMqOptions rabbitMq, RabbitMqTopologyOptions topology)
    {
        if (string.IsNullOrWhiteSpace(rabbitMq.HostName))
        {
            throw new InvalidOperationException("RabbitMq:HostName is required.");
        }

        if (string.IsNullOrWhiteSpace(topology.PriceScrapedExchange))
        {
            throw new InvalidOperationException("RabbitMqTopology:PriceScrapedExchange is required.");
        }

        if (string.IsNullOrWhiteSpace(topology.PriceScrapedQueue))
        {
            throw new InvalidOperationException("RabbitMqTopology:PriceScrapedQueue is required.");
        }

        if (string.IsNullOrWhiteSpace(topology.DealDetectedExchange))
        {
            throw new InvalidOperationException("RabbitMqTopology:DealDetectedExchange is required.");
        }
    }
}
