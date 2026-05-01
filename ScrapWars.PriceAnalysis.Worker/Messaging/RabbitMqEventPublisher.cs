using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ScrapWars.PriceAnalysis.Worker.Messaging;

public class RabbitMqEventPublisher : IEventPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IOptions<RabbitMqOptions> _rabbitMqOptions;

    public RabbitMqEventPublisher(IOptions<RabbitMqOptions> rabbitMqOptions)
    {
        _rabbitMqOptions = rabbitMqOptions;
    }

    public Task PublishAsync<TMessage>(string exchange, string routingKey, TMessage message, CancellationToken cancellationToken = default)
    {
        var options = _rabbitMqOptions.Value;

        var factory = new ConnectionFactory
        {
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(exchange, ExchangeType.Fanout, durable: true, autoDelete: false);

        var payload = JsonSerializer.SerializeToUtf8Bytes(message, SerializerOptions);
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        channel.BasicPublish(exchange, routingKey, properties, payload);

        return Task.CompletedTask;
    }
}
