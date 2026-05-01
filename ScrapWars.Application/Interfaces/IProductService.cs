using ScrapWars.Domain.Entities;

namespace ScrapWars.Application.Interfaces;

public interface IProductService
{
    Task<Product> AddProductAsync(string name, string link, string categoryName, ulong guildId);
    Task<IReadOnlyCollection<Product>> GetProductsAsync(ulong guildId);
    Task<IReadOnlyCollection<Product>> GetAllProductsAsync();
    Task<Product?> GetProductByNameAsync(string name, ulong guildId);
    Task<bool> DeleteProductAsync(string name, ulong guildId);
}
