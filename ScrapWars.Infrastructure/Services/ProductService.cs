using Microsoft.EntityFrameworkCore;
using ScrapWars.Application.Interfaces;
using ScrapWars.Domain.Entities;
using ScrapWars.Infrastructure.Persistence;

namespace ScrapWars.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly ScrapWarsDbContext _dbContext;
    private readonly IGuildSubscriptionService _guildSubscriptionService;

    public ProductService(
        ScrapWarsDbContext dbContext,
        IGuildSubscriptionService guildSubscriptionService)
    {
        _dbContext = dbContext;
        _guildSubscriptionService = guildSubscriptionService;
    }

    public async Task<Product> AddProductAsync(string name, string link, string categoryName, ulong guildId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(link);
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryName);

        var trimmedName = name.Trim();
        var trimmedLink = link.Trim();
        var normalizedName = trimmedName.ToLowerInvariant();
        var normalizedCategoryName = ProductCategory.NormalizeName(categoryName);

        var category = await _dbContext.ProductCategories.FirstOrDefaultAsync(category =>
            category.GuildId == guildId &&
            category.NormalizedName == normalizedCategoryName);

        if (category is null)
        {
            throw new InvalidOperationException($"Category '{categoryName.Trim()}' does not exist in this server.");
        }

        var existingProduct = await _dbContext.Products.AnyAsync(product =>
            product.GuildId == guildId &&
            product.Name.ToLower() == normalizedName);

        if (existingProduct)
        {
            throw new InvalidOperationException($"Product '{trimmedName}' already exists in this server.");
        }

        await _guildSubscriptionService.EnsureProductCapacityAsync(guildId);

        var product = new Product(trimmedName, trimmedLink, guildId, category.Id);
        product.Category = category;

        _dbContext.Products.Add(product);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UX_products_guild_id_name_lower", StringComparison.Ordinal) == true)
        {
            throw new InvalidOperationException($"Product '{trimmedName}' already exists in this server.", ex);
        }

        return product;
    }

    public async Task<IReadOnlyCollection<Product>> GetProductsAsync(ulong guildId)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Where(product => product.GuildId == guildId)
            .OrderBy(product => product.Name)
            .ToArrayAsync();
    }

    public async Task<IReadOnlyCollection<Product>> GetAllProductsAsync()
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .OrderBy(product => product.GuildId)
            .ThenBy(product => product.Name)
            .ToArrayAsync();
    }

    public async Task<Product?> GetProductByNameAsync(string name, ulong guildId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalizedName = name.Trim().ToLowerInvariant();

        return await _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .FirstOrDefaultAsync(product =>
                product.GuildId == guildId &&
                product.Name.ToLower() == normalizedName);
    }

    public async Task<bool> DeleteProductAsync(string name, ulong guildId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalizedName = name.Trim().ToLowerInvariant();
        var product = await _dbContext.Products.FirstOrDefaultAsync(product =>
            product.GuildId == guildId &&
            product.Name.ToLower() == normalizedName);

        if (product is null)
        {
            return false;
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync();

        return true;
    }
}
