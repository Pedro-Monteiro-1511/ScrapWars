# Code Reference

## Domain

`ScrapWars.Domain/Entities/Product.cs`

```csharp
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Link { get; set; }
    public ulong GuildId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

## Application

`ScrapWars.Application/Interfaces/IBotService.cs`

```csharp
public interface IBotService
{
    Task StartAsync(string token, CancellationToken cancellationToken);
    Task StopAsync();
}
```

`ScrapWars.Application/Interfaces/IProductService.cs`

```csharp
public interface IProductService
{
    Task<Product> AddProductAsync(string name, string link, ulong guildId);
    Task<IReadOnlyCollection<Product>> GetProductsAsync(ulong guildId);
    Task<bool> DeleteProductAsync(string name, ulong guildId);
}
```

## Infrastructure

`ScrapWars.Infrastructure/ExternalServices/BotService.cs`

- Creates the NetCord `GatewayClient`.
- Adds command modules from the Infrastructure assembly.
- Registers slash commands when the Discord gateway is ready.
- Executes slash command interactions through `ApplicationCommandService` and the app `IServiceProvider`.
- Reads optional `Discord:GuildId` from configuration for guild-scoped command registration.

`ScrapWars.Infrastructure/Commands/ProductModule.cs`

- `/product-add`: creates a product in the current guild under a configured category.
- `/product-list`: lists products in the current guild.
- `/product-delete`: deletes a product by name from the current guild.

`ScrapWars.Infrastructure/Commands/ConfigurationModule.cs`

- `/category-create`: creates a guild-scoped category.
- `/category-list`: lists guild-scoped categories.
- `/category-delete`: deletes an empty guild-scoped category.
- `/category-channel-add`: routes category notifications to a Discord channel.
- `/category-channel-remove`: removes a category/channel route.
- `/category-channel-list`: lists category notification channels.

`ScrapWars.Infrastructure/Services/ProductService.cs`

- Stores products in Supabase Postgres through `ScrapWarsDbContext`.
- Prevents duplicate product names in the same guild.
- Requires product adds to reference an existing guild category.
- Reads and writes products with EF Core async queries.

`ScrapWars.Infrastructure/Services/GuildConfigurationService.cs`

- Manages guild-scoped product categories.
- Manages category-to-channel notification routes.
- Prevents duplicate category names per guild.
- Prevents duplicate channel routes per category.

`ScrapWars.Infrastructure/Persistence/ScrapWarsDbContext.cs`

- Maps `Product` to the `products` table.
- Maps `ProductCategory` to the `product_categories` table.
- Maps `CategoryNotificationChannel` to the `category_notification_channels` table.
- Stores Discord guild IDs as `numeric(20,0)` for safe unsigned 64-bit values.
- Adds guild lookup indexes and uniqueness constraints.

`ScrapWars.Infrastructure/Persistence/Migrations/`

- Contains the initial EF Core migration for Supabase/Postgres.

## Worker

`ScrapWars.Worker/Program.cs`

Registers:

- `ApplicationCommandService<ApplicationCommandContext>`
- `ScrapWarsDbContext`
- `IGuildConfigurationService -> GuildConfigurationService` as a scoped service
- `IProductService -> ProductService` as a scoped service
- `IBotService -> BotService`
- `Worker`

`ScrapWars.Worker/Worker.cs`

- Reads `Discord:Token`.
- Starts the bot.
- Keeps the worker alive until cancellation.
- Stops the bot during shutdown.
