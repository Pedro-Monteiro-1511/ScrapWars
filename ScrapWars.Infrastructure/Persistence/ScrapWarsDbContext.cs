using Microsoft.EntityFrameworkCore;
using ScrapWars.Domain.Entities;

namespace ScrapWars.Infrastructure.Persistence;

public class ScrapWarsDbContext : DbContext
{
    public ScrapWarsDbContext(DbContextOptions<ScrapWarsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<CategoryNotificationChannel> CategoryNotificationChannels => Set<CategoryNotificationChannel>();
    public DbSet<GuildSubscription> GuildSubscriptions => Set<GuildSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureGuildSubscriptions(modelBuilder);
        ConfigureProductCategories(modelBuilder);
        ConfigureCategoryNotificationChannels(modelBuilder);
        ConfigureProducts(modelBuilder);
    }

    private static void ConfigureGuildSubscriptions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GuildSubscription>(entity =>
        {
            entity.ToTable("guild_subscriptions");

            entity.HasKey(subscription => subscription.Id);

            entity.Property(subscription => subscription.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(subscription => subscription.GuildId)
                .HasColumnName("guild_id")
                .HasColumnType("numeric(20,0)")
                .HasConversion(
                    guildId => (decimal)guildId,
                    guildId => (ulong)guildId);

            entity.Property(subscription => subscription.Plan)
                .HasColumnName("plan")
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(subscription => subscription.ExtraChannels)
                .HasColumnName("extra_channels")
                .IsRequired();

            entity.Property(subscription => subscription.ExtraCategories)
                .HasColumnName("extra_categories")
                .IsRequired();

            entity.Property(subscription => subscription.ExtraProducts)
                .HasColumnName("extra_products")
                .IsRequired();

            entity.Property(subscription => subscription.ExtraChannelUnitPrice)
                .HasColumnName("extra_channel_unit_price")
                .HasColumnType("numeric(10,2)")
                .IsRequired();

            entity.Property(subscription => subscription.ExtraCategoryUnitPrice)
                .HasColumnName("extra_category_unit_price")
                .HasColumnType("numeric(10,2)")
                .IsRequired();

            entity.Property(subscription => subscription.ExtraProductUnitPrice)
                .HasColumnName("extra_product_unit_price")
                .HasColumnType("numeric(10,2)")
                .IsRequired();

            entity.Property(subscription => subscription.CurrencyCode)
                .HasColumnName("currency_code")
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(subscription => subscription.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(subscription => subscription.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone");

            entity.HasIndex(subscription => subscription.GuildId)
                .IsUnique()
                .HasDatabaseName("UX_guild_subscriptions_guild_id");
        });
    }

    private static void ConfigureProducts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");

            entity.HasKey(product => product.Id);

            entity.Property(product => product.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(product => product.Name)
                .HasColumnName("name")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(product => product.Link)
                .HasColumnName("link")
                .HasMaxLength(2048)
                .IsRequired();

            entity.Property(product => product.GuildId)
                .HasColumnName("guild_id")
                .HasColumnType("numeric(20,0)")
                .HasConversion(
                    guildId => (decimal)guildId,
                    guildId => (ulong)guildId);

            entity.Property(product => product.CategoryId)
                .HasColumnName("category_id");

            entity.Property(product => product.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(product => product.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone");

            entity.HasIndex(product => product.GuildId)
                .HasDatabaseName("IX_products_guild_id");

            entity.HasIndex(product => product.CategoryId)
                .HasDatabaseName("IX_products_category_id");

            entity.HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureProductCategories(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.ToTable("product_categories");

            entity.HasKey(category => category.Id);

            entity.Property(category => category.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(category => category.GuildId)
                .HasColumnName("guild_id")
                .HasColumnType("numeric(20,0)")
                .HasConversion(
                    guildId => (decimal)guildId,
                    guildId => (ulong)guildId);

            entity.Property(category => category.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(category => category.NormalizedName)
                .HasColumnName("normalized_name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(category => category.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(category => category.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone");

            entity.HasIndex(category => category.GuildId)
                .HasDatabaseName("IX_product_categories_guild_id");

            entity.HasIndex(category => new { category.GuildId, category.NormalizedName })
                .IsUnique()
                .HasDatabaseName("UX_product_categories_guild_id_normalized_name");
        });
    }

    private static void ConfigureCategoryNotificationChannels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CategoryNotificationChannel>(entity =>
        {
            entity.ToTable("category_notification_channels");

            entity.HasKey(channel => channel.Id);

            entity.Property(channel => channel.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(channel => channel.GuildId)
                .HasColumnName("guild_id")
                .HasColumnType("numeric(20,0)")
                .HasConversion(
                    guildId => (decimal)guildId,
                    guildId => (ulong)guildId);

            entity.Property(channel => channel.CategoryId)
                .HasColumnName("category_id")
                .IsRequired();

            entity.Property(channel => channel.ChannelId)
                .HasColumnName("channel_id")
                .HasColumnType("numeric(20,0)")
                .HasConversion(
                    channelId => (decimal)channelId,
                    channelId => (ulong)channelId);

            entity.Property(channel => channel.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.HasIndex(channel => channel.GuildId)
                .HasDatabaseName("IX_category_notification_channels_guild_id");

            entity.HasIndex(channel => new { channel.CategoryId, channel.ChannelId })
                .IsUnique()
                .HasDatabaseName("UX_category_notification_channels_category_id_channel_id");

            entity.HasOne(channel => channel.Category)
                .WithMany(category => category.NotificationChannels)
                .HasForeignKey(channel => channel.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
