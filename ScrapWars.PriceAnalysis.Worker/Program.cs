using Microsoft.EntityFrameworkCore;
using ScrapWars.PriceAnalysis.Worker;
using ScrapWars.PriceAnalysis.Worker.Messaging;
using ScrapWars.PriceAnalysis.Worker.Persistence;
using ScrapWars.PriceAnalysis.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.Configure<RabbitMqTopologyOptions>(builder.Configuration.GetSection(RabbitMqTopologyOptions.SectionName));
builder.Services.Configure<PriceAnalysisOptions>(builder.Configuration.GetSection(PriceAnalysisOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("Supabase");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:Supabase is not configured. Set it before starting the price analysis worker.");
}

builder.Services.AddDbContext<PriceHistoryDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
        npgsqlOptions.MigrationsAssembly(typeof(PriceHistoryDbContext).Assembly.FullName)));

builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddScoped<PriceHistoryAnalysisService>();
builder.Services.AddHostedService<PriceAnalysisWorker>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PriceHistoryDbContext>();
    await dbContext.Database.MigrateAsync();
}

await host.RunAsync();
