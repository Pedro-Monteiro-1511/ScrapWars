# Docker Setup

This repository includes a Docker Compose setup for the event-driven price monitoring pipeline:

- `rabbitmq`
- `bot-worker`
- `scraper-worker`
- `price-analysis-worker`
- `notifications-worker`

## 1. Prepare environment variables

Copy `.env.example` to `.env` and fill in the real values:

```powershell
Copy-Item .env.example .env
```

Required values:

- `SUPABASE_CONNECTION_STRING`
- `DISCORD_BOT_TOKEN`
- `DISCORD_GUILD_ID` for fast slash-command registration in your test server

## 2. Start the stack

```powershell
docker compose up --build
```

Run in the background:

```powershell
docker compose up --build -d
```

## 3. RabbitMQ

RabbitMQ management UI:

- URL: `http://localhost:15672`
- Username: `guest`
- Password: `guest`

## 4. Stop the stack

```powershell
docker compose down
```

To also remove the RabbitMQ volume:

```powershell
docker compose down -v
```

## 5. What this starts

`scraper-worker`

- Waits for `ProductPriceCheckRequestedEvent`
- Scrapes the supported site (`idealista.pt` is the first one implemented)
- Publishes `ProductPriceScrapedEvent`

`bot-worker`

- Registers slash commands in Discord
- Stores products/categories/configuration in Supabase
- Publishes `ProductPriceCheckRequestedEvent` to RabbitMQ

`price-analysis-worker`

- Consumes `ProductPriceScrapedEvent`
- Stores history in `product_price_history`
- Publishes `ProductDealDetectedEvent` when a deal is found

`notifications-worker`

- Consumes `ProductDealDetectedEvent`
- Looks up the configured category channels in Supabase
- Sends the Discord notification

## 6. Triggering the flow

Once the stack is up and the bot is in your Discord server:

- use `/product-add` to save a product and queue its first check
- use `/product-check` to queue one product manually
- use `/product-check-all` to queue every product in the server

Current scraper support:

- `idealista.pt`: extracts business type (`Venda` / `Arrendamento`) and the current listing price
