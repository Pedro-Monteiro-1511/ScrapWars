# ScrapWars Bot - Quick Start

ScrapWars is a .NET worker service that runs a Discord bot with NetCord slash commands for tracking product links per Discord server.

## Commands

```text
/product-add name:<name> link:<url>
/product-list
/product-delete name:<name>
/category-create name:<name>
/category-list
/category-delete name:<name>
/category-channel-add category:<name> channel:<channel>
/category-channel-remove category:<name> channel:<channel>
/category-channel-list category:<name>
```

## Configuration

Set the Discord token and Supabase connection string outside source control with user secrets or environment variables:

```powershell
dotnet user-secrets set "Discord:Token" "YOUR_BOT_TOKEN" --project ScrapWars.Worker
dotnet user-secrets set "Discord:GuildId" "YOUR_TEST_SERVER_ID" --project ScrapWars.Worker
dotnet user-secrets set "ConnectionStrings:Supabase" "Host=YOUR_SUPABASE_HOST;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true" --project ScrapWars.Worker
```

`Discord:GuildId` is optional. When set, commands are registered to that server and update quickly. When empty, commands are registered globally and can take longer to appear in Discord.

## Database

The app uses EF Core with Npgsql against Supabase Postgres.

Apply migrations after the Supabase database is created:

```powershell
dotnet ef database update --project ScrapWars.Infrastructure --startup-project ScrapWars.Worker --context ScrapWarsDbContext
```

Create future migrations with:

```powershell
dotnet ef migrations add MigrationName --project ScrapWars.Infrastructure --startup-project ScrapWars.Worker --context ScrapWarsDbContext --output-dir Persistence\Migrations
```

## Build And Run

```powershell
dotnet build -c Release
dotnet run --project ScrapWars.Worker -c Release
```

## Project Structure

```text
ScrapWars.Domain/
  Entities/Product.cs
  Entities/ProductCategory.cs
  Entities/CategoryNotificationChannel.cs

ScrapWars.Application/
  Interfaces/IBotService.cs
  Interfaces/IGuildConfigurationService.cs
  Interfaces/IProductService.cs

ScrapWars.Infrastructure/
  Commands/ConfigurationModule.cs
  Commands/ProductModule.cs
  ExternalServices/BotService.cs
  Persistence/ScrapWarsDbContext.cs
  Persistence/Migrations/
  Services/ProductService.cs

ScrapWars.Worker/
  Program.cs
  Worker.cs
  appsettings.json
```

## Current Storage

Products, categories, and category notification channels are stored in Supabase Postgres and scoped by Discord guild ID.
