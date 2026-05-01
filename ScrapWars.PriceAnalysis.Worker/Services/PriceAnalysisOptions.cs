namespace ScrapWars.PriceAnalysis.Worker.Services;

public class PriceAnalysisOptions
{
    public const string SectionName = "PriceAnalysis";

    public int RecentWindowDays { get; set; } = 7;
    public int SuperDealWindowDays { get; set; } = 180;
}
