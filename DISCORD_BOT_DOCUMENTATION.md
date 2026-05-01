# ScrapWars Discord Bot

ScrapWars uses NetCord to expose Discord slash commands from a .NET worker service.

## Commands

### `/product-add`

Adds a product link to the current Discord server.

Parameters:

- `name`: Product name.
- `link`: Product URL.

### `/product-list`

Lists product links registered for the current Discord server. The current response shows up to 10 products to keep the interaction response compact.

### `/product-delete`

Deletes a product link from the current Discord server by name.

Parameter:

- `name`: Product name to delete.

### `/channel-register`

Registers a Discord channel for future SuperDeals notifications.

Parameter:

- `channel`: Discord channel selected from the slash command UI.

## Architecture

The solution follows a small layered structure:

- `ScrapWars.Domain`: entity models, currently `Product`.
- `ScrapWars.Application`: service contracts, including `IBotService` and `IProductService`.
- `ScrapWars.Infrastructure`: NetCord integration, slash command modules, and in-memory product storage.
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
  "Discord": {
    "Token": "",
    "GuildId": "",
    "StartupChannelId": ""
  }
}
```

Use `Discord:GuildId` for a development server if you want command updates to appear quickly.

## Notes

- Commands are Discord slash commands.
- The Discord library is NetCord.
- Product storage is currently in-memory and per guild.
- The bot token that used to be in config should be considered exposed and rotated in the Discord Developer Portal.
