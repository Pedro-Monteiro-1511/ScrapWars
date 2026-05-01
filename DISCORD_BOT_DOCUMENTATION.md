# ScrapWars Discord Bot

ScrapWars uses NetCord to expose Discord slash commands from a .NET worker service.

## Commands

### `/product-add`

Adds a product link to the current Discord server under a configured category.

Parameters:

- `name`: Product name.
- `link`: Product URL.
- `category`: Existing category name for the current server.

### `/product-list`

Lists product links registered for the current Discord server. The current response shows up to 10 products to keep the interaction response compact.

### `/product-delete`

Deletes a product link from the current Discord server by name.

Parameter:

- `name`: Product name to delete.

### `/category-create`

Creates a product category for the current Discord server.

Parameter:

- `name`: Category name.

### `/category-list`

Lists categories configured for the current Discord server.

### `/category-delete`

Deletes an empty category from the current Discord server.

Parameter:

- `name`: Category name.

### `/category-channel-add`

Configures a channel to receive notifications for a category.

Parameters:

- `category`: Existing category name.
- `channel`: Discord channel selected from the slash command UI.

### `/category-channel-remove`

Removes a channel from a category notification route.

Parameters:

- `category`: Existing category name.
- `channel`: Discord channel selected from the slash command UI.

### `/category-channel-list`

Lists channels configured to receive notifications for a category.

Parameter:

- `category`: Existing category name.

## Architecture

The solution follows a small layered structure:

- `ScrapWars.Domain`: entity models, currently `Product`.
- `ScrapWars.Application`: service contracts, including `IBotService` and `IProductService`.
- `ScrapWars.Infrastructure`: NetCord integration, slash command modules, EF Core persistence, and migrations.
- `ScrapWars.Worker`: hosted service entry point and dependency injection setup.

## NetCord Flow

```text
Worker starts
  -> BotService creates GatewayClient
  -> BotService scans Infrastructure assembly for command modules
  -> Ready event registers slash commands with Discord
  -> InteractionCreate event creates ApplicationCommandContext
  -> ApplicationCommandService executes the matching module through DI
  -> Module returns a Discord interaction response
```

## Configuration

`ScrapWars.Worker/appsettings.json` contains empty placeholders only. Keep real secrets in user secrets or environment variables.

```json
{
  "ConnectionStrings": {
    "Supabase": ""
  },
  "Discord": {
    "Token": "",
    "GuildId": "",
    "StartupChannelId": ""
  }
}
```

Use `Discord:GuildId` for a development server if you want command updates to appear quickly.

Set Supabase locally with:

```powershell
dotnet user-secrets set "ConnectionStrings:Supabase" "Host=YOUR_SUPABASE_HOST;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true" --project ScrapWars.Worker
```

## Migrations

The migrations create product, category, and category notification channel tables, all scoped by Discord guild ID.

```powershell
dotnet ef database update --project ScrapWars.Infrastructure --startup-project ScrapWars.Worker --context ScrapWarsDbContext
```

## Notes

- Commands are Discord slash commands.
- The Discord library is NetCord.
- Product/category storage is Supabase Postgres through EF Core and Npgsql.
- The bot token that used to be in config should be considered exposed and rotated in the Discord Developer Portal.
