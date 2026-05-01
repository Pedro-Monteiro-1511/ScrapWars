using System;

namespace ScrapWars.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Link { get; set; }
    public ulong GuildId { get; set; }
    public Guid? CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ProductCategory? Category { get; set; }

    public Product()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        Name = string.Empty;
        Link = string.Empty;
        GuildId = 0;
    }

    public Product(string name, string link, ulong guildId, Guid categoryId) : this()
    {
        Name = name;
        Link = link;
        GuildId = guildId;
        CategoryId = categoryId;
    }
}
