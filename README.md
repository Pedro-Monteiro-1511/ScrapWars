# ScrapWars

ScrapWars is a Discord-based price monitoring system built as a portfolio project around event-driven architecture in `.NET`.

The project tracks products across multiple websites, stores price history, detects deals automatically, and routes notifications to Discord channels by category and guild.

## Why This Project Exists

This repository is meant to show:

- practical multi-project `.NET` architecture
- Discord slash-command integration with `NetCord`
- message-driven workflows with `RabbitMQ`
- scraping with `Playwright`
- persistence with `EF Core` and `PostgreSQL`
- separation of concerns between command handling, scraping, analysis, and notifications

## Core Features

- Guild-scoped product tracking
- Category-based routing of notifications
- Price scraping across multiple supported sites
- Historical price storage and deal detection
- Scheduled price checks at `08:00` and `20:00`
- Manual checks through `/product-check` and `/product-check-all`
- Discount percentage capture when a site exposes old price vs current price
- Subscription-aware limits for channels, categories, and products

## Supported Scrapers

- `worten.pt`
- `idealista.pt`
- `pcdiga.com`

## User-Facing Commands

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

ScrapWars is split into small workers that communicate through RabbitMQ events:

1. `ScrapWars.Worker`
   - hosts the Discord bot
   - registers slash commands
   - stores guild/product/category configuration
   - publishes price-check requests

2. `ScrapWars.Scraper.Worker`
   - consumes price-check requests
   - selects the correct site scraper by product URL
   - uses Playwright to fetch the latest product price
   - publishes scraped price events

3. `ScrapWars.PriceAnalysis.Worker`
   - stores price history
   - compares the new price against previous history
   - detects deals and super deals
   - publishes deal events

4. `ScrapWars.Notifications.Worker`
   - resolves the target Discord channels for the product category
   - sends the notification message

## Event Flow

```text
Discord command / scheduler
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

## Project Structure

```text
ScrapWars.Domain/                 Core entities and business primitives
ScrapWars.Application/            Contracts, interfaces, shared DTOs
ScrapWars.Contracts/              RabbitMQ event contracts shared by workers
ScrapWars.Infrastructure/         Discord modules, EF persistence, services, publishers
ScrapWars.Worker/                 Discord bot host and scheduled check worker
ScrapWars.Scraper.Worker/         Playwright scraping pipeline
ScrapWars.PriceAnalysis.Worker/   Price history persistence and deal analysis
ScrapWars.Notifications.Worker/   Discord notification delivery
```

## Technical Highlights

- `PlaywrightBrowserProvider` keeps browser lifecycle centralized for scraper reuse
- Site selection is resolved through a scraper registry keyed by URL host
- Historical reads and writes are split so the bot can query the latest known price without owning the analysis workflow
- `/product-check-all` now compares the last stored price against the fresh result returned by the async pipeline
- Docker Compose orchestration brings up the bot and all background workers as one system

## Main Stack

- `.NET 10`
- `NetCord`
- `RabbitMQ`
- `Playwright`
- `Entity Framework Core`
- `PostgreSQL / Supabase`
- `Docker Compose`
