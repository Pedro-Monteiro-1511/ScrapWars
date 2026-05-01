using ScrapWars.Domain.Entities;

namespace ScrapWars.Application.DTO;

public class GuildEntitlementSummary
{
    public ulong GuildId { get; init; }
    public GuildSubscriptionPlan Plan { get; init; }
    public string PlanDisplayName { get; init; } = string.Empty;
    public int IncludedChannels { get; init; }
    public int IncludedCategories { get; init; }
    public int IncludedProducts { get; init; }
    public int ExtraChannels { get; init; }
    public int ExtraCategories { get; init; }
    public int ExtraProducts { get; init; }
    public int UsedChannels { get; init; }
    public int UsedCategories { get; init; }
    public int UsedProducts { get; init; }
    public decimal ExtraChannelUnitPrice { get; init; }
    public decimal ExtraCategoryUnitPrice { get; init; }
    public decimal ExtraProductUnitPrice { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;

    public int TotalChannels => IncludedChannels + ExtraChannels;
    public int TotalCategories => IncludedCategories + ExtraCategories;
    public int TotalProducts => IncludedProducts + ExtraProducts;
    public int RemainingChannels => Math.Max(0, TotalChannels - UsedChannels);
    public int RemainingCategories => Math.Max(0, TotalCategories - UsedCategories);
    public int RemainingProducts => Math.Max(0, TotalProducts - UsedProducts);
}
