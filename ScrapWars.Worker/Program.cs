using Microsoft.EntityFrameworkCore;
using NetCord.Services.ApplicationCommands;
using ScrapWars.Application.Interfaces;
using ScrapWars.Infrastructure.ExternalServices;
using ScrapWars.Infrastructure.Messaging;
using ScrapWars.Infrastructure.Persistence;
using ScrapWars.Infrastructure.Services;
using ScrapWars.Worker;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration.GetConnectionString("Supabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Supabase is not configured. Set it in user secrets, environment variables, or appsettings before starting the bot.");
        }

        services.AddSingleton<ApplicationCommandService<ApplicationCommandContext>>();
        services.Configure<RabbitMqOptions>(context.Configuration.GetSection(RabbitMqOptions.SectionName));
        services.Configure<RabbitMqTopologyOptions>(context.Configuration.GetSection(RabbitMqTopologyOptions.SectionName));
        services.Configure<ScheduledPriceCheckOptions>(context.Configuration.GetSection(ScheduledPriceCheckOptions.SectionName));
        services.AddDbContext<ScrapWarsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.MigrationsAssembly(typeof(ScrapWarsDbContext).Assembly.FullName)));
        services.AddDbContext<ProductPriceHistoryReadDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddHttpClient<IDirectMessageService, DiscordDirectMessageService>();
        services.AddScoped<IGuildSubscriptionService, GuildSubscriptionService>();
        services.AddScoped<IGuildConfigurationService, GuildConfigurationService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductPriceHistoryService, ProductPriceHistoryService>();
        services.AddSingleton<IPriceCheckRequestPublisher, RabbitMqPriceCheckRequestPublisher>();
        services.AddSingleton<IBotService, BotService>();

        services.AddHostedService<Worker>();
        services.AddHostedService<ScheduledPriceCheckWorker>();
    })
    .Build();

await host.RunAsync();
