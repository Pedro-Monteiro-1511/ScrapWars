using ScrapWars.Application.Interfaces;
using ScrapWars.Domain.Entities;

namespace ScrapWars.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly List<Product> _products = [];
    private readonly object _lock = new();

    public Task<Product> AddProductAsync(string name, string link, ulong guildId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(link);

        lock (_lock)
        {
            var existingProduct = _products.Any(product =>
                product.GuildId == guildId &&
                string.Equals(product.Name, name, StringComparison.OrdinalIgnoreCase));

            if (existingProduct)
            {
                throw new InvalidOperationException($"Product '{name}' already exists in this server.");
            }

            var product = new Product(name.Trim(), link.Trim(), guildId);
            _products.Add(product);

            return Task.FromResult(product);
        }
    }

    public Task<IReadOnlyCollection<Product>> GetProductsAsync(ulong guildId)
    {
        lock (_lock)
        {
            var products = _products
                .Where(product => product.GuildId == guildId)
                .OrderBy(product => product.Name)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<Product>>(products);
        }
    }

    public Task<bool> DeleteProductAsync(string name, ulong guildId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        lock (_lock)
        {
            var product = _products.FirstOrDefault(product =>
                product.GuildId == guildId &&
                string.Equals(product.Name, name, StringComparison.OrdinalIgnoreCase));

            if (product is null)
            {
                return Task.FromResult(false);
            }

            _products.Remove(product);
            return Task.FromResult(true);
        }
    }
}
