namespace ScrapWars.Domain.Entities;

public class GuildSubscription
{
    public Guid Id { get; set; }
    public ulong GuildId { get; set; }
    public GuildSubscriptionPlan Plan { get; set; }
    public int ExtraChannels { get; set; }
    public int ExtraCategories { get; set; }
    public int ExtraProducts { get; set; }
    public decimal ExtraChannelUnitPrice { get; set; }
    public decimal ExtraCategoryUnitPrice { get; set; }
    public decimal ExtraProductUnitPrice { get; set; }
    public string CurrencyCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public GuildSubscription()
    {
        Id = Guid.NewGuid();
        Plan = GuildSubscriptionPlan.Free;
        CurrencyCode = GuildPlanCatalog.DefaultCurrencyCode;
        ExtraChannelUnitPrice = GuildPlanCatalog.DefaultAddOnUnitPrice;
        ExtraCategoryUnitPrice = GuildPlanCatalog.DefaultAddOnUnitPrice;
        ExtraProductUnitPrice = GuildPlanCatalog.DefaultAddOnUnitPrice;
        CreatedAt = DateTime.UtcNow;
    }

    public GuildSubscription(ulong guildId)
        : this()
    {
        GuildId = guildId;
    }

    public void Configure(
        GuildSubscriptionPlan plan,
        int extraChannels,
        int extraCategories,
        int extraProducts,
        decimal extraChannelUnitPrice,
        decimal extraCategoryUnitPrice,
        decimal extraProductUnitPrice,
        string currencyCode)
    {
        if (extraChannels < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(extraChannels));
        }

        if (extraCategories < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(extraCategories));
        }

        if (extraProducts < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(extraProducts));
        }

        if (extraChannelUnitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(extraChannelUnitPrice));
        }

        if (extraCategoryUnitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(extraCategoryUnitPrice));
        }

        if (extraProductUnitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(extraProductUnitPrice));
        }

        Plan = plan;
        ExtraChannels = extraChannels;
        ExtraCategories = extraCategories;
        ExtraProducts = extraProducts;
        ExtraChannelUnitPrice = extraChannelUnitPrice;
        ExtraCategoryUnitPrice = extraCategoryUnitPrice;
        ExtraProductUnitPrice = extraProductUnitPrice;
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode)
            ? GuildPlanCatalog.DefaultCurrencyCode
            : currencyCode.Trim().ToUpperInvariant();
        UpdatedAt = DateTime.UtcNow;
    }
}
