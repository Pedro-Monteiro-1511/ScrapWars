using NetCord.Services.ApplicationCommands;
using ScrapWars.Application.DTO;
using ScrapWars.Application.Interfaces;

namespace ScrapWars.Infrastructure.Commands;

public class ProductModule : ApplicationCommandModule<ApplicationCommandContext>
{
    private static readonly TimeSpan CheckAllWaitTimeout = TimeSpan.FromSeconds(90);

    private readonly IProductService _productService;
    private readonly IProductPriceHistoryService _productPriceHistoryService;
    private readonly IPriceCheckRequestPublisher _priceCheckRequestPublisher;

    public ProductModule(
        IProductService productService,
        IProductPriceHistoryService productPriceHistoryService,
        IPriceCheckRequestPublisher priceCheckRequestPublisher)
    {
        _productService = productService;
        _productPriceHistoryService = productPriceHistoryService;
        _priceCheckRequestPublisher = priceCheckRequestPublisher;
    }

    [SlashCommand("product-add", "Add a product link to this server")]
    public async Task<string> AddProductAsync(
        [SlashCommandParameter(Name = "name", Description = "Name of the product")] string name,
        [SlashCommandParameter(Name = "link", Description = "Link to the product")] string link,
        [SlashCommandParameter(Name = "category", Description = "Configured category for this product")] string category)
    {
        if (!TryGetGuildId(out var guildId, out var error))
        {
            return error;
        }

        try
        {
            var product = await _productService.AddProductAsync(name, link, category, guildId);

            try
            {
                await _priceCheckRequestPublisher.PublishAsync(product);
                return $"Product added and queued for price check: {product.Name} [{category.Trim()}] - {product.Link}";
            }
            catch (Exception ex)
            {
                return $"Product added, but the price check request could not be queued right now: {ex.Message}";
            }
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
            .Select(product => $"- {product.Name} [{product.Category?.Name ?? "uncategorized"}]: {product.Link}");

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

    [SlashCommand("product-check", "Queue a price check for one product in this server")]
    public async Task<string> CheckProductAsync(
        [SlashCommandParameter(Name = "name", Description = "Name of the product to check")] string name)
    {
        if (!TryGetGuildId(out var guildId, out var error))
        {
            return error;
        }

        var product = await _productService.GetProductByNameAsync(name, guildId);

        if (product is null)
        {
            return $"Product not found: {name}";
        }

        try
        {
            var latestSnapshot = await _productPriceHistoryService.GetLatestSnapshotsAsync(new[] { product.Id });
            latestSnapshot.TryGetValue(product.Id, out var snapshot);

            await _priceCheckRequestPublisher.PublishAsync(product, snapshot?.Price);
            return $"Price check queued for: {product.Name}";
        }
        catch (Exception ex)
        {
            return $"Could not queue the price check right now: {ex.Message}";
        }
    }

    [SlashCommand("product-check-all", "Queue a price check for all products in this server")]
    public async Task<string> CheckAllProductsAsync()
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

        var productIds = products.Select(product => product.Id).ToArray();
        var commandStartedAtUtc = DateTime.UtcNow;
        var previousSnapshots = await _productPriceHistoryService.GetLatestSnapshotsAsync(productIds);
        var failedProducts = new List<string>();

        foreach (var product in products)
        {
            try
            {
                previousSnapshots.TryGetValue(product.Id, out var snapshot);
                await _priceCheckRequestPublisher.PublishAsync(product, snapshot?.Price);
            }
            catch
            {
                failedProducts.Add(product.Name);
            }
        }

        var queuedProducts = products
            .Where(product => !failedProducts.Contains(product.Name, StringComparer.Ordinal))
            .ToArray();

        if (queuedProducts.Length == 0)
        {
            return $"Could not queue price checks for any of the {products.Count} products.";
        }

        var currentSnapshots = await _productPriceHistoryService.WaitForUpdatedSnapshotsAsync(
            queuedProducts.Select(product => product.Id),
            commandStartedAtUtc,
            CheckAllWaitTimeout);

        var lines = new List<string>();

        foreach (var product in queuedProducts)
        {
            previousSnapshots.TryGetValue(product.Id, out var previousSnapshot);
            currentSnapshots.TryGetValue(product.Id, out var currentSnapshot);

            if (currentSnapshot is null || currentSnapshot.CapturedAtUtc < commandStartedAtUtc)
            {
                lines.Add($"- {product.Name}: sem resposta nova ainda");
                continue;
            }

            lines.Add(FormatPriceLine(product.Name, previousSnapshot, currentSnapshot));
        }

        var response = $"Price checks executed for {queuedProducts.Length} products:{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";

        if (failedProducts.Count > 0)
        {
            var failedSummary = string.Join(", ", failedProducts.Take(5));
            var failedSuffix = failedProducts.Count > 5 ? ", ..." : string.Empty;
            response += $"{Environment.NewLine}Failed to queue {failedProducts.Count}: {failedSummary}{failedSuffix}";
        }

        return TrimDiscordResponse(response, lines.Count);
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

    private static string FormatPriceLine(string productName, ProductPriceSnapshot? previousSnapshot, ProductPriceSnapshot currentSnapshot)
    {
        var previousText = previousSnapshot is null
            ? "sem historico"
            : $"{previousSnapshot.Price:F2} {previousSnapshot.Currency}";

        var currentText = $"{currentSnapshot.Price:F2} {currentSnapshot.Currency}";
        var discountText = currentSnapshot.DiscountPercentage.HasValue
            ? $" | desconto: {currentSnapshot.DiscountPercentage.Value:F2}%"
            : string.Empty;

        return $"- {productName}: {previousText} -> {currentText}{discountText}";
    }

    private static string TrimDiscordResponse(string response, int lineCount)
    {
        const int maxLength = 1800;

        if (response.Length <= maxLength)
        {
            return response;
        }

        var truncated = response[..maxLength];
        return $"{truncated}{Environment.NewLine}... output truncated ({lineCount} products processed).";
    }
}
