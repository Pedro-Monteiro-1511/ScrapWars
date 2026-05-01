namespace ScrapWars.Domain.Entities;

public sealed record GuildPlanDefinition(
    GuildSubscriptionPlan Plan,
    string DisplayName,
    int ChannelsIncluded,
    int CategoriesIncluded,
    int ProductsIncluded);

public static class GuildPlanCatalog
{
    public const decimal DefaultAddOnUnitPrice = 0.99m;
    public const string DefaultCurrencyCode = "EUR";

    private static readonly IReadOnlyDictionary<GuildSubscriptionPlan, GuildPlanDefinition> Plans =
        new Dictionary<GuildSubscriptionPlan, GuildPlanDefinition>
        {
            [GuildSubscriptionPlan.Free] = new(GuildSubscriptionPlan.Free, "Base / Free", 2, 2, 5),
            [GuildSubscriptionPlan.Pro] = new(GuildSubscriptionPlan.Pro, "Pro", 5, 3, 10),
            [GuildSubscriptionPlan.Max] = new(GuildSubscriptionPlan.Max, "Max", 10, 6, 15)
        };

    public static GuildPlanDefinition GetDefinition(GuildSubscriptionPlan plan)
    {
        return Plans.TryGetValue(plan, out var definition)
            ? definition
            : throw new ArgumentOutOfRangeException(nameof(plan), plan, "Unsupported guild subscription plan.");
    }
}
