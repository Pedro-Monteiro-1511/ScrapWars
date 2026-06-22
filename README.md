# ScrapWars

ScrapWars is a distributed Discord bot for product price monitoring.

The system tracks product URLs from multiple websites, scrapes current prices, stores historical data, detects deals, and routes notifications to Discord channels based on category and guild configuration.

## Purpose

This project was built as a portfolio piece to demonstrate:

- multi-project `.NET` architecture
- event-driven backend design
- browser automation with `Playwright`
- asynchronous processing with `RabbitMQ`
- relational persistence with `EF Core` and `PostgreSQL`
- Discord bot integration with `NetCord`

## What The System Does

- stores tracked products per Discord guild
- groups products by configurable categories
- maps categories to notification channels
- queues manual price checks through slash commands
- runs scheduled price checks twice per day
- scrapes supported websites with site-specific handlers
- stores price history for each product
- calculates discount percentage when the source exposes prior price data
- detects deals from historical comparisons
- sends Discord notifications when a deal is found

## Supported Sources

- `worten.pt`
- `idealista.pt`
- `pcdiga.com`

## Commands

```text
/help
/product-add
/product-list
/product-delete
/product-check
/product-check-all
/category-create
/category-list
/category-delete
/category-channel-add
/category-channel-remove
/category-channel-list
```

## Architecture

ScrapWars is split into independent workers connected through RabbitMQ:

### `ScrapWars.Worker`

Main Discord bot host.

Responsibilities:
- registers slash commands
- manages guild-scoped product and category data
- publishes price-check requests
- schedules automatic checks at `08:00` and `20:00`

### `ScrapWars.Scraper.Worker`

Scraping pipeline.

Responsibilities:
- consumes price-check requests
- resolves the correct scraper by URL host
- launches Playwright browser contexts
- extracts current price, business type, currency, and discount percentage
- publishes normalized scrape results

### `ScrapWars.PriceAnalysis.Worker`

Historical analysis pipeline.

Responsibilities:
- stores every scraped price in `product_price_history`
- compares the latest value with previous records
- detects regular deals and super deals
- publishes deal events for downstream consumers

### `ScrapWars.Notifications.Worker`

Notification delivery pipeline.

Responsibilities:
- resolves Discord channels configured for the product category
- formats deal messages
- sends notifications to Discord

## How The Pieces Connect

```text
Discord command / scheduled worker
        ->
ProductPriceCheckRequestedEvent
        ->
Scraper worker
        ->
ProductPriceScrapedEvent
        ->
Price analysis worker
        ->
ProductDealDetectedEvent
        ->
Notifications worker
        ->
Discord channel message
```

This split keeps scraping, persistence, analysis, and delivery isolated from the command layer. Each stage has a narrow responsibility and communicates through explicit event contracts.

## Project Structure

```text
ScrapWars.Domain/                 Core entities and business rules
ScrapWars.Application/            Interfaces and shared DTOs
ScrapWars.Contracts/              Integration event contracts
ScrapWars.Infrastructure/         Discord modules, persistence, services, publishers
ScrapWars.Worker/                 Discord bot host and scheduling
ScrapWars.Scraper.Worker/         Playwright-based scraping workers
ScrapWars.PriceAnalysis.Worker/   Price history and deal analysis
ScrapWars.Notifications.Worker/   Notification routing and Discord delivery
```

## Technical Notes

- Scraper selection is handled through a registry keyed by product host.
- Each site scraper is isolated behind a common `ISiteScraper` contract.
- Price history is written by the analysis worker and read by the bot through a dedicated read model.
- `/product-check-all` compares the previously stored price with the fresh value returned by the asynchronous pipeline.
- Discount percentage is propagated across scraping, persistence, analysis, and notification layers.
- Docker Compose is used to run the bot and all workers as one distributed system.

## Stack

- `.NET 10`
- `NetCord`
- `RabbitMQ`
- `Playwright`
- `Entity Framework Core`
- `PostgreSQL / Supabase`
- `Docker Compose`
