using Microsoft.EntityFrameworkCore;
using ScrapWars.Notifications.Worker;
using ScrapWars.Notifications.Worker.Data;
using ScrapWars.Notifications.Worker.Discord;
using ScrapWars.Notifications.Worker.Messaging;
using ScrapWars.Notifications.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.Configure<RabbitMqTopologyOptions>(builder.Configuration.GetSection(RabbitMqTopologyOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("Supabase");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:Supabase is not configured. Set it before starting the notifications worker.");
}

builder.Services.AddDbContext<NotificationRoutingDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddHttpClient<IDiscordChannelNotifier, DiscordChannelNotifier>();
builder.Services.AddScoped<DealNotificationService>();
builder.Services.AddHostedService<DealNotificationWorker>();

var host = builder.Build();
host.Run();
