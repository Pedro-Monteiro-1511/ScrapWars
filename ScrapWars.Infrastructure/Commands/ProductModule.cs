using NetCord.Services.ApplicationCommands;
using ScrapWars.Application.Interfaces;

namespace ScrapWars.Infrastructure.Commands;

public class ProductModule : ApplicationCommandModule<ApplicationCommandContext>
{
    private readonly IProductService _productService;

    public ProductModule(IProductService productService)
    {
        _productService = productService;
    }

    [SlashCommand("product-add", "Add a product link to this server")]
    public async Task<string> AddProductAsync(
        [SlashCommandParameter(Name = "name", Description = "Name of the product")] string name,
        [SlashCommandParameter(Name = "link", Description = "Link to the product")] string link)
    {
        if (!TryGetGuildId(out var guildId, out var error))
        {
            return error;
        }

        try
        {
            var product = await _productService.AddProductAsync(name, link, guildId);

            return $"Product added: {product.Name} - {product.Link}";
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message;
        }
    }

    [SlashCommand("product-list", "List product links registered in this server")]
    public async Task<string> ListProductsAsync()
    {
        if (!TryGetGuildId(out var guildId, out var error))
        {
            return error;
        }

        var products = await _productService.GetProductsAsync(guildId);

        if (products.Count == 0)
        {
            return "No products registered in this server yet.";
        }

        var productLines = products
            .Take(10)
            .Select(product => $"- {product.Name}: {product.Link}");

        var suffix = products.Count > 10
            ? $"{Environment.NewLine}Showing 10 of {products.Count} products."
            : string.Empty;

        return $"Products registered in this server:{Environment.NewLine}{string.Join(Environment.NewLine, productLines)}{suffix}";
    }

    [SlashCommand("product-delete", "Delete a product link from this server")]
    public async Task<string> DeleteProductAsync(
        [SlashCommandParameter(Name = "name", Description = "Name of the product to delete")] string name)
    {
        if (!TryGetGuildId(out var guildId, out var error))
        {
            return error;
        }

        var deleted = await _productService.DeleteProductAsync(name, guildId);

        return deleted
            ? $"Product deleted: {name}"
            : $"Product not found: {name}";
    }

    private bool TryGetGuildId(out ulong guildId, out string error)
    {
        guildId = Context.Interaction.GuildId ?? 0;
        error = string.Empty;

        if (guildId == 0)
        {
            error = "This command must be used inside a Discord server.";
            return false;
        }

        return true;
    }
}
