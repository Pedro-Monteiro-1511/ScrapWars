namespace ScrapWars.Domain.Entities;

public class ProductCategory
{
    public Guid Id { get; set; }
    public ulong GuildId { get; set; }
    public string Name { get; set; }
    public string NormalizedName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ICollection<Product> Products { get; set; }
    public ICollection<CategoryNotificationChannel> NotificationChannels { get; set; }

    public ProductCategory()
    {
        Id = Guid.NewGuid();
        Name = string.Empty;
        NormalizedName = string.Empty;
        CreatedAt = DateTime.UtcNow;
        Products = [];
        NotificationChannels = [];
    }

    public ProductCategory(string name, ulong guildId)
        : this()
    {
        Name = name.Trim();
        NormalizedName = NormalizeName(name);
        GuildId = guildId;
    }

    public static string NormalizeName(string name)
    {
        return name.Trim().ToLowerInvariant();
    }
}
