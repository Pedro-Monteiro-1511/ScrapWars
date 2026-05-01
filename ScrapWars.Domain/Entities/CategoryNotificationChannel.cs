namespace ScrapWars.Domain.Entities;

public class CategoryNotificationChannel
{
    public Guid Id { get; set; }
    public ulong GuildId { get; set; }
    public Guid CategoryId { get; set; }
    public ulong ChannelId { get; set; }
    public DateTime CreatedAt { get; set; }
    public ProductCategory? Category { get; set; }

    public CategoryNotificationChannel()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    public CategoryNotificationChannel(ulong guildId, Guid categoryId, ulong channelId)
        : this()
    {
        GuildId = guildId;
        CategoryId = categoryId;
        ChannelId = channelId;
    }
}
