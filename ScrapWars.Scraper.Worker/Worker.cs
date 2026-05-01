using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ScrapWars.Contracts.Events;
using ScrapWars.Scraper.Worker.Messaging;
using ScrapWars.Scraper.Worker.Scraping;

namespace ScrapWars.Scraper.Worker;

public class PriceCheckWorker : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<PriceCheckWorker> _logger;
    private readonly IOptions<RabbitMqOptions> _rabbitMqOptions;
    private readonly IOptions<RabbitMqTopologyOptions> _topologyOptions;
    private readonly ProductPriceScrapingService _scrapingService;
    private readonly IEventPublisher _eventPublisher;

    public PriceCheckWorker(
        ILogger<PriceCheckWorker> logger,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IOptions<RabbitMqTopologyOptions> topologyOptions,
        ProductPriceScrapingService scrapingService,
        IEventPublisher eventPublisher)
    {
        _logger = logger;
        _rabbitMqOptions = rabbitMqOptions;
        _topologyOptions = topologyOptions;
        _scrapingService = scrapingService;
        _eventPublisher = eventPublisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitMq = _rabbitMqOptions.Value;
        var topology = _topologyOptions.Value;

        ValidateConfiguration(rabbitMq, topology);

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

        channel.QueueDeclare(
            queue: topology.PriceCheckRequestedQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.ExchangeDeclare(
            exchange: topology.PriceScrapedExchange,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null);

        channel.ExchangeDeclare(
            exchange: topology.PriceScrapeFailedExchange,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null);

        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, eventArgs) =>
        {
            await HandleMessageAsync(channel, eventArgs, topology, stoppingToken);
        };

        var consumerTag = channel.BasicConsume(
            queue: topology.PriceCheckRequestedQueue,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation(
            "Scraper worker listening on queue '{Queue}' with scraped exchange '{ScrapedExchange}'.",
            topology.PriceCheckRequestedQueue,
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
                    "RabbitMQ is not reachable for the scraper worker yet. Retrying in {DelaySeconds} seconds...",
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
        ProductPriceCheckRequestedEvent? message = null;

        try
        {
            message = JsonSerializer.Deserialize<ProductPriceCheckRequestedEvent>(
                Encoding.UTF8.GetString(eventArgs.Body.ToArray()),
                SerializerOptions);

            if (message is null)
            {
                _logger.LogWarning("Received an empty scraping request payload.");
                channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                return;
            }

            var scrapedEvent = await _scrapingService.ScrapeAsync(message, cancellationToken);

            await _eventPublisher.PublishAsync(
                topology.PriceScrapedExchange,
                topology.PriceScrapedRoutingKey,
                scrapedEvent,
                cancellationToken);

            channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
        }
        catch (UnsupportedSiteException ex) when (message is not null)
        {
            await PublishFailureAsync(message, topology, ex.Message, cancellationToken);
            channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing a price check request.");

            if (message is not null)
            {
                await PublishFailureAsync(message, topology, ex.Message, cancellationToken);
                channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                return;
            }

            channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
        }
    }

    private async Task PublishFailureAsync(
        ProductPriceCheckRequestedEvent request,
        RabbitMqTopologyOptions topology,
        string reason,
        CancellationToken cancellationToken)
    {
        var failureEvent = new ProductPriceScrapeFailedEvent
        {
            EventId = Guid.NewGuid(),
            CorrelationId = request.CorrelationId,
            ProductId = request.ProductId,
            CategoryId = request.CategoryId,
            CategoryName = request.CategoryName,
            GuildId = request.GuildId,
            ProductName = request.ProductName,
            ProductUrl = request.ProductUrl,
            FailureReason = reason,
            OccurredAtUtc = DateTime.UtcNow
        };

        await _eventPublisher.PublishAsync(
            topology.PriceScrapeFailedExchange,
            topology.PriceScrapeFailedRoutingKey,
            failureEvent,
            cancellationToken);
    }

    private static void ValidateConfiguration(RabbitMqOptions rabbitMq, RabbitMqTopologyOptions topology)
    {
        if (string.IsNullOrWhiteSpace(rabbitMq.HostName))
        {
            throw new InvalidOperationException("RabbitMq:HostName is required.");
        }

        if (string.IsNullOrWhiteSpace(topology.PriceCheckRequestedQueue))
        {
            throw new InvalidOperationException("RabbitMqTopology:PriceCheckRequestedQueue is required.");
        }

        if (string.IsNullOrWhiteSpace(topology.PriceScrapedExchange))
        {
            throw new InvalidOperationException("RabbitMqTopology:PriceScrapedExchange is required.");
        }

        if (string.IsNullOrWhiteSpace(topology.PriceScrapeFailedExchange))
        {
            throw new InvalidOperationException("RabbitMqTopology:PriceScrapeFailedExchange is required.");
        }
    }
}
