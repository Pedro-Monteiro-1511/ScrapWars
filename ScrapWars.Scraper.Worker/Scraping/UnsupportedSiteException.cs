namespace ScrapWars.Scraper.Worker.Scraping;

public class UnsupportedSiteException : Exception
{
    public UnsupportedSiteException(string message)
        : base(message)
    {
    }
}
