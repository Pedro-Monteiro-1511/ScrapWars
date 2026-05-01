namespace ScrapWars.PriceAnalysis.Worker.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<TMessage>(string exchange, string routingKey, TMessage message, CancellationToken cancellationToken = default);
}
