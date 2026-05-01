using ScrapWars.Domain.Entities;

namespace ScrapWars.Application.Interfaces;

public interface IPriceCheckRequestPublisher
{
    Task PublishAsync(Product product, decimal? lastKnownPrice = null, CancellationToken cancellationToken = default);
}
