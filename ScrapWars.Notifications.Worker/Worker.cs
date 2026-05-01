using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ScrapWars.Contracts.Events;
using ScrapWars.Notifications.Worker.Messaging;
using ScrapWars.Notifications.Worker.Services;

namespace ScrapWars.Notifications.Worker;

public class DealNotificationWorker : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<DealNotificationWorker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOptions<RabbitMqOptions> _rabbitMqOptions;
    private readonly IOptions<RabbitMqTopologyOptions> _topologyOptions;

    public DealNotificationWorker(
        ILogger<DealNotificationWorker> logger,
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

        channel.ExchangeDeclare(topology.DealDetectedExchange, ExchangeType.Fanout, durable: true, autoDelete: false);
        channel.QueueDeclare(topology.DealDetectedQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(topology.DealDetectedQueue, topology.DealDetectedExchange, routingKey: string.Empty);
        channel.BasicQos(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, eventArgs) => await HandleMessageAsync(channel, eventArgs, stoppingToken);

        var consumerTag = channel.BasicConsume(topology.DealDetectedQueue, autoAck: false, consumer: consumer);

        _logger.LogInformation(
            "Notifications worker listening on queue '{Queue}' bound to exchange '{Exchange}'.",
            topology.DealDetectedQueue,
            topology.DealDetectedExchange);

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
                    "RabbitMQ is not reachable for the notifications worker yet. Retrying in {DelaySeconds} seconds...",
                    delaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }
        }

        throw new OperationCanceledException(cancellationToken);
    }

    private async Task HandleMessageAsync(IModel channel, BasicDeliverEventArgs eventArgs, CancellationToken cancellationToken)
    {
        try
        {
            var message = JsonSerializer.Deserialize<ProductDealDetectedEvent>(
                Encoding.UTF8.GetString(eventArgs.Body.ToArray()),
                SerializerOptions);

            if (message is null)
            {
                _logger.LogWarning("Received an empty deal detected payload.");
                channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                return;
            }

            using var scope = _serviceScopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<DealNotificationService>();

            await notificationService.NotifyAsync(message, cancellationToken);

            channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing a deal notification event.");
            channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
        }
    }

    private static void ValidateConfiguration(RabbitMqOptions rabbitMq, RabbitMqTopologyOptions topology)
    {
        if (string.IsNullOrWhiteSpace(rabbitMq.HostName))
        {
            throw new InvalidOperationException("RabbitMq:HostName is required.");
        }

        if (string.IsNullOrWhiteSpace(topology.DealDetectedExchange))
        {
            throw new InvalidOperationException("RabbitMqTopology:DealDetectedExchange is required.");
        }

        if (string.IsNullOrWhiteSpace(topology.DealDetectedQueue))
        {
            throw new InvalidOperationException("RabbitMqTopology:DealDetectedQueue is required.");
        }
    }
}
