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

- `/product-add`: creates a product in the current guild.
- `/product-list`: lists products in the current guild.
- `/product-delete`: deletes a product by name from the current guild.

`ScrapWars.Infrastructure/Commands/ChannelModule.cs`

- `/channel-register`: accepts a Discord channel and returns a confirmation. Persistence is still a future step.

`ScrapWars.Infrastructure/Services/ProductService.cs`

- Stores products in memory.
- Prevents duplicate product names in the same guild.
- Uses a lock around the backing list because the service is a singleton.

## Worker

`ScrapWars.Worker/Program.cs`

Registers:

- `ApplicationCommandService<ApplicationCommandContext>`
- `IProductService -> ProductService`
- `IBotService -> BotService`
- `Worker`

`ScrapWars.Worker/Worker.cs`

- Reads `Discord:Token`.
- Starts the bot.
- Keeps the worker alive until cancellation.
- Stops the bot during shutdown.
