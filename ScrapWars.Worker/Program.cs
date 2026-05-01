using Microsoft.EntityFrameworkCore;
using NetCord.Services.ApplicationCommands;
using ScrapWars.Application.Interfaces;
using ScrapWars.Infrastructure.ExternalServices;
using ScrapWars.Infrastructure.Persistence;
using ScrapWars.Infrastructure.Services;
using ScrapWars.Worker;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration.GetConnectionString("Supabase")
            ?? throw new InvalidOperationException("ConnectionStrings:Supabase is not configured.");

        services.AddSingleton<ApplicationCommandService<ApplicationCommandContext>>();
        services.AddDbContext<ScrapWarsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.MigrationsAssembly(typeof(ScrapWarsDbContext).Assembly.FullName)));
        services.AddHttpClient<IDirectMessageService, DiscordDirectMessageService>();
        services.AddScoped<IGuildSubscriptionService, GuildSubscriptionService>();
        services.AddScoped<IGuildConfigurationService, GuildConfigurationService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddSingleton<IBotService, BotService>();

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
