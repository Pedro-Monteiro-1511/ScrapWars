using Microsoft.EntityFrameworkCore;
using ScrapWars.PriceAnalysis.Worker.Persistence.Entities;

namespace ScrapWars.PriceAnalysis.Worker.Persistence;

public class PriceHistoryDbContext : DbContext
{
    public PriceHistoryDbContext(DbContextOptions<PriceHistoryDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProductPriceHistoryEntry> ProductPriceHistory => Set<ProductPriceHistoryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductPriceHistoryEntry>(entity =>
        {
            entity.ToTable("product_price_history");

            entity.HasKey(item => item.Id);

            entity.Property(item => item.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(item => item.SourceEventId)
                .HasColumnName("source_event_id")
                .IsRequired();

            entity.Property(item => item.ProductId)
                .HasColumnName("product_id")
                .IsRequired();

            entity.Property(item => item.CategoryId)
                .HasColumnName("category_id");

            entity.Property(item => item.CategoryName)
                .HasColumnName("category_name")
                .HasMaxLength(100);

            entity.Property(item => item.GuildId)
                .HasColumnName("guild_id")
                .HasColumnType("numeric(20,0)")
                .HasConversion(
                    guildId => (decimal)guildId,
                    guildId => (ulong)guildId);

            entity.Property(item => item.ProductName)
                .HasColumnName("product_name")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(item => item.ProductUrl)
                .HasColumnName("product_url")
                .HasMaxLength(2048)
                .IsRequired();

            entity.Property(item => item.SiteName)
                .HasColumnName("site_name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(item => item.BusinessType)
                .HasColumnName("business_type")
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(item => item.Price)
                .HasColumnName("price")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            entity.Property(item => item.DiscountPercentage)
                .HasColumnName("discount_percentage")
                .HasColumnType("numeric(5,2)");

            entity.Property(item => item.Currency)
                .HasColumnName("currency")
                .HasMaxLength(8)
                .IsRequired();

            entity.Property(item => item.CapturedAtUtc)
                .HasColumnName("captured_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(item => item.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.HasIndex(item => item.SourceEventId)
                .IsUnique()
                .HasDatabaseName("UX_product_price_history_source_event_id");

            entity.HasIndex(item => new { item.ProductId, item.CapturedAtUtc })
                .HasDatabaseName("IX_product_price_history_product_id_captured_at_utc");
        });
    }
}
