using NetCord.Services.ApplicationCommands;
using ScrapWars.Application.Interfaces;
using ScrapWars.Infrastructure.ExternalServices;
using ScrapWars.Infrastructure.Services;
using ScrapWars.Worker;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {

        services.AddSingleton<ApplicationCommandService<ApplicationCommandContext>>();
        services.AddSingleton<IProductService, ProductService>();
        services.AddSingleton<IBotService, BotService>();

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
