using Microsoft.EntityFrameworkCore;

namespace ScrapWars.Infrastructure.Persistence;

public class ProductPriceHistoryReadDbContext : DbContext
{
    public ProductPriceHistoryReadDbContext(DbContextOptions<ProductPriceHistoryReadDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProductPriceHistoryReadModel> ProductPriceHistory => Set<ProductPriceHistoryReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductPriceHistoryReadModel>(entity =>
        {
            entity.ToTable("product_price_history");

            entity.HasKey(item => item.Id);

            entity.Property(item => item.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(item => item.ProductId)
                .HasColumnName("product_id")
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

            entity.HasIndex(item => new { item.ProductId, item.CapturedAtUtc })
                .HasDatabaseName("IX_product_price_history_product_id_captured_at_utc");
        });
    }
}
