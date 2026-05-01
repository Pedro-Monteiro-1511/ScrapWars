# ScrapWars Bot - Quick Start

ScrapWars is a .NET worker service that runs a Discord bot with NetCord slash commands for tracking product links per Discord server.

## Commands

```text
/product-add name:<name> link:<url>
/product-list
/product-delete name:<name>
/channel-register channel:<channel>
```

## Configuration

Set the Discord token outside source control with user secrets or environment variables:

```powershell
dotnet user-secrets set "Discord:Token" "YOUR_BOT_TOKEN" --project ScrapWars.Worker
dotnet user-secrets set "Discord:GuildId" "YOUR_TEST_SERVER_ID" --project ScrapWars.Worker
```

`Discord:GuildId` is optional. When set, commands are registered to that server and update quickly. When empty, commands are registered globally and can take longer to appear in Discord.

## Build And Run

```powershell
dotnet build -c Release
dotnet run --project ScrapWars.Worker -c Release
```

## Project Structure

```text
ScrapWars.Domain/
  Entities/Product.cs

ScrapWars.Application/
  Interfaces/IBotService.cs
  Interfaces/IProductService.cs

ScrapWars.Infrastructure/
  Commands/ChannelModule.cs
  Commands/ProductModule.cs
  ExternalServices/BotService.cs
  Services/ProductService.cs

ScrapWars.Worker/
  Program.cs
  Worker.cs
  appsettings.json
```

## Current Storage

Products are stored in memory by `ProductService`, grouped by Discord guild ID. This is useful for development, but products are lost when the worker restarts. A database-backed implementation should replace it before production use.
