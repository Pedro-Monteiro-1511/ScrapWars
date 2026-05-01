using ScrapWars.Scraper.Worker;
using ScrapWars.Scraper.Worker.Messaging;
using ScrapWars.Scraper.Worker.Scraping;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.Configure<RabbitMqTopologyOptions>(builder.Configuration.GetSection(RabbitMqTopologyOptions.SectionName));
builder.Services.Configure<ScrapingOptions>(builder.Configuration.GetSection(ScrapingOptions.SectionName));

builder.Services.AddHttpClient();
builder.Services.AddSingleton<PlaywrightBrowserProvider>();
builder.Services.AddSingleton<ISiteScraper, IdealistaSiteScraper>();
builder.Services.AddSingleton<ISiteScraper, PcdigaSiteScraper>();
builder.Services.AddSingleton<ISiteScraper, WortenSiteScraper>();
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddSingleton<ISiteScraperRegistry, SiteScraperRegistry>();
builder.Services.AddSingleton<ProductPriceScrapingService>();
builder.Services.AddHostedService<PriceCheckWorker>();

var host = builder.Build();
host.Run();
