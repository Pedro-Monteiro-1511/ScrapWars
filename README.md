# ScrapWars

ScrapWars is a Discord bot built with `.NET`, `NetCord`, `EF Core`, and `Supabase Postgres`.

The bot is designed for multi-server use, with all data scoped by Discord guild. It lets each server organize tracked products by category, define which channels receive notifications for which categories, and enforce plan-based limits for categories, channels, and products.

## What It Does

- Registers Discord slash commands with NetCord
- Stores products per guild
- Lets each guild create and manage categories
- Maps notification channels to categories
- Sends a `/help` response by private message
- Supports guild subscription plans with add-on capacity

## Current Plans

- `Base / Free`: 2 channels, 2 categories, 5 products
- `Pro`: 5 channels, 3 categories, 10 products
- `Max`: 10 channels, 6 categories, 15 products
- Extra channel/category/product capacity: `0.99 EUR` each

## Main Commands

```text
/help
/product-add
/product-list
/product-delete
/category-create
/category-list
/category-delete
/category-channel-add
/category-channel-remove
/category-channel-list
```

## Stack

- `.NET 10`
- `NetCord`
- `Entity Framework Core`
- `Npgsql`
- `Supabase Postgres`

## Project Structure

```text
ScrapWars.Domain/          Core entities and plan definitions
ScrapWars.Application/     Service contracts and DTOs
ScrapWars.Infrastructure/  NetCord commands, services, EF persistence, migrations
ScrapWars.Worker/          Host startup and worker entry point
```

## Local Setup

Set secrets locally:

```powershell
dotnet user-secrets set "Discord:Token" "YOUR_BOT_TOKEN" --project ScrapWars.Worker
dotnet user-secrets set "Discord:GuildId" "YOUR_TEST_GUILD_ID" --project ScrapWars.Worker
dotnet user-secrets set "ConnectionStrings:Supabase" "Host=YOUR_HOST;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true" --project ScrapWars.Worker
```

Apply migrations:

```powershell
dotnet ef database update --project ScrapWars.Infrastructure --startup-project ScrapWars.Worker --context ScrapWarsDbContext
```

Build and run:

```powershell
dotnet build -c Release
dotnet run --project ScrapWars.Worker -c Release
```

## Notes

- `Discord:GuildId` is optional, but useful during development because guild command registration updates faster than global registration.
- Real secrets should stay out of source control.
- The repository also includes more detailed setup notes in [QUICK_START.md](QUICK_START.md) and [DISCORD_BOT_DOCUMENTATION.md](DISCORD_BOT_DOCUMENTATION.md).
