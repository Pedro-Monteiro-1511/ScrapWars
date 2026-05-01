using Microsoft.EntityFrameworkCore;
using ScrapWars.Notifications.Worker.Data.ReadModels;

namespace ScrapWars.Notifications.Worker.Data;

public class NotificationRoutingDbContext : DbContext
{
    public NotificationRoutingDbContext(DbContextOptions<NotificationRoutingDbContext> options)
        : base(options)
    {
    }

    public DbSet<CategoryNotificationChannelReadModel> CategoryNotificationChannels => Set<CategoryNotificationChannelReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CategoryNotificationChannelReadModel>(entity =>
        {
            entity.ToTable("category_notification_channels");
            entity.HasKey(item => item.Id);

            entity.Property(item => item.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(item => item.GuildId)
                .HasColumnName("guild_id")
                .HasColumnType("numeric(20,0)")
                .HasConversion(
                    guildId => (decimal)guildId,
                    guildId => (ulong)guildId);

            entity.Property(item => item.CategoryId)
                .HasColumnName("category_id")
                .IsRequired();

            entity.Property(item => item.ChannelId)
                .HasColumnName("channel_id")
                .HasColumnType("numeric(20,0)")
                .HasConversion(
                    channelId => (decimal)channelId,
                    channelId => (ulong)channelId);
        });
    }
}
