using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ScrapWars.Application.Interfaces;
using ScrapWars.Contracts.Events;
using ScrapWars.Domain.Entities;

namespace ScrapWars.Infrastructure.Messaging;

public class RabbitMqPriceCheckRequestPublisher : IPriceCheckRequestPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IOptions<RabbitMqOptions> _rabbitMqOptions;
    private readonly IOptions<RabbitMqTopologyOptions> _topologyOptions;

    public RabbitMqPriceCheckRequestPublisher(
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IOptions<RabbitMqTopologyOptions> topologyOptions)
    {
        _rabbitMqOptions = rabbitMqOptions;
        _topologyOptions = topologyOptions;
    }

    public Task PublishAsync(Product product, decimal? lastKnownPrice = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        var rabbitMq = _rabbitMqOptions.Value;
        var topology = _topologyOptions.Value;

        if (string.IsNullOrWhiteSpace(rabbitMq.HostName))
        {
            throw new InvalidOperationException("RabbitMq:HostName is required to publish price check requests.");
        }

        if (string.IsNullOrWhiteSpace(topology.PriceCheckRequestedQueue))
        {
            throw new InvalidOperationException("RabbitMqTopology:PriceCheckRequestedQueue is required to publish price check requests.");
        }

        var message = new ProductPriceCheckRequestedEvent
        {
            EventId = Guid.NewGuid(),
            ProductId = product.Id,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty,
            GuildId = product.GuildId,
            ProductName = product.Name,
            ProductUrl = product.Link,
            LastKnownPrice = lastKnownPrice,
            Currency = "EUR",
            RequestedAtUtc = DateTime.UtcNow
        };

        var connectionFactory = new ConnectionFactory
        {
            HostName = rabbitMq.HostName,
            Port = rabbitMq.Port,
            UserName = rabbitMq.UserName,
            Password = rabbitMq.Password,
            VirtualHost = rabbitMq.VirtualHost
        };

        using var connection = connectionFactory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: topology.PriceCheckRequestedQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var payload = JsonSerializer.SerializeToUtf8Bytes(message, SerializerOptions);
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: topology.PriceCheckRequestedQueue,
            basicProperties: properties,
            body: payload);

        return Task.CompletedTask;
    }
}
