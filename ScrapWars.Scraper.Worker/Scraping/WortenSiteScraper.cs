using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Playwright;
using ScrapWars.Contracts.Events;

namespace ScrapWars.Scraper.Worker.Scraping;

public class WortenSiteScraper : ISiteScraper
{
    private readonly PlaywrightBrowserProvider _browserProvider;

    public WortenSiteScraper(PlaywrightBrowserProvider browserProvider)
    {
        _browserProvider = browserProvider;
    }

    public string SiteName => "worten.pt";

    public bool CanHandle(Uri productUri)
    {
        return productUri.Host.Contains("worten.pt", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ScrapedProductResult> ScrapeAsync(Uri productUri, CancellationToken cancellationToken = default)
    {
        var browser = await _browserProvider.GetBrowserAsync(cancellationToken);
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            Locale = "pt-PT",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                ["Accept-Language"] = "pt-PT,pt;q=0.9,en-US;q=0.8,en;q=0.7"
            },
            ViewportSize = new ViewportSize
            {
                Width = 1440,
                Height = 1200
            }
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(productUri.ToString(), new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 45_000
        });

        await WaitForPriceAsync(page);
        await page.WaitForTimeoutAsync(750);

        var html = await page.ContentAsync();
        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(html, cancellationToken);

        var price = TryGetPriceFromStructuredData(document)
            ?? TryGetPriceFromSelectors(document)
            ?? throw new InvalidOperationException("Could not extract the current product price from worten.pt.");
        var previousPrice = TryGetPreviousPriceFromSelectors(document, price);

        return new ScrapedProductResult
        {
            SiteName = SiteName,
            BusinessType = ListingBusinessType.Sale,
            CurrentPrice = price,
            DiscountPercentage = CalculateDiscountPercentage(price, previousPrice),
            Currency = DetectCurrency(document),
            CapturedAtUtc = DateTime.UtcNow
        };
    }

    private static decimal? TryGetPriceFromStructuredData(IDocument document)
    {
        foreach (var script in document.QuerySelectorAll("script[type='application/ld+json']"))
        {
            var content = script.TextContent;

            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            try
            {
                using var jsonDocument = JsonDocument.Parse(content);
                var price = FindPrice(jsonDocument.RootElement);

                if (price.HasValue)
                {
                    return price.Value;
                }
            }
            catch (JsonException)
            {
            }
        }

        return null;
    }

    private static decimal? TryGetPreviousPriceFromSelectors(IDocument document, decimal currentPrice)
    {
        var selectors = new[]
        {
            "s",
            "del",
            "[class*='old-price']",
            "[class*='list-price']",
            "[class*='was-price']",
            "[class*='strike']"
        };

        foreach (var selector in selectors)
        {
            foreach (var node in document.QuerySelectorAll(selector))
            {
                var rawValue = node.GetAttribute("content") ?? node.TextContent;

                if (TryParsePrice(rawValue, out var price) && price > currentPrice)
                {
                    return price;
                }
            }
        }

        return null;
    }

    private static decimal? FindPrice(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if ((property.NameEquals("price") || property.NameEquals("lowPrice")) &&
                    TryParsePrice(property.Value.ToString(), out var parsedPrice))
                {
                    return parsedPrice;
                }

                var nestedPrice = FindPrice(property.Value);

                if (nestedPrice.HasValue)
                {
                    return nestedPrice;
                }
            }
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nestedPrice = FindPrice(item);

                if (nestedPrice.HasValue)
                {
                    return nestedPrice;
                }
            }
        }

        return null;
    }

    private static decimal? TryGetPriceFromSelectors(IDocument document)
    {
        var selectors = new[]
        {
            "meta[property='product:price:amount']",
            "meta[itemprop='price']",
            "[data-testid='price-current']",
            "[class*='price']",
            "p",
            "span",
            "div"
        };

        foreach (var selector in selectors)
        {
            foreach (var node in document.QuerySelectorAll(selector))
            {
                var rawValue = node.GetAttribute("content") ?? node.TextContent;

                if (TryParsePrice(rawValue, out var price))
                {
                    return price;
                }
            }
        }

        return null;
    }

    private static string DetectCurrency(IDocument document)
    {
        var currencyCandidates = new[]
        {
            document.QuerySelector("meta[itemprop='priceCurrency']")?.GetAttribute("content"),
            document.QuerySelector("meta[property='product:price:currency']")?.GetAttribute("content"),
            document.Body?.TextContent
        };

        foreach (var candidate in currencyCandidates.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            if (candidate!.Contains("\u20AC", StringComparison.Ordinal) ||
                candidate.Contains("EUR", StringComparison.OrdinalIgnoreCase) ||
                candidate.Contains("euros", StringComparison.OrdinalIgnoreCase))
            {
                return "EUR";
            }
        }

        return "EUR";
    }

    private static bool TryParsePrice(string? rawValue, out decimal price)
    {
        price = 0;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        if (!rawValue.Contains('\u20AC') &&
            !rawValue.Contains("EUR", StringComparison.OrdinalIgnoreCase) &&
            !Regex.IsMatch(rawValue, @"\d+[.,]\d{2}"))
        {
            return false;
        }

        var match = Regex.Match(rawValue, @"\d[\d\.\,\s]*");
        var numeric = match.Value;

        if (string.IsNullOrWhiteSpace(numeric))
        {
            return false;
        }

        if (!numeric.Contains(',') && !numeric.Contains('.') && numeric.Length > 2)
        {
            var normalizedIntegerCents = numeric.Replace(" ", string.Empty, StringComparison.Ordinal);

            if (decimal.TryParse(normalizedIntegerCents, NumberStyles.Integer, CultureInfo.InvariantCulture, out var centsValue))
            {
                price = centsValue / 100m;
                return true;
            }
        }

        numeric = numeric.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace(",", ".", StringComparison.Ordinal);

        return decimal.TryParse(numeric, NumberStyles.Number, CultureInfo.InvariantCulture, out price);
    }

    private static decimal? CalculateDiscountPercentage(decimal currentPrice, decimal? previousPrice)
    {
        if (!previousPrice.HasValue || previousPrice.Value <= currentPrice || previousPrice.Value <= 0)
        {
            return null;
        }

        var discount = ((previousPrice.Value - currentPrice) / previousPrice.Value) * 100m;
        return Math.Round(discount, 2, MidpointRounding.AwayFromZero);
    }

    private static async Task WaitForPriceAsync(IPage page)
    {
        var selectors = new[]
        {
            "meta[property='product:price:amount']",
            "meta[itemprop='price']",
            "[data-testid='price-current']",
            "[class*='price']"
        };

        foreach (var selector in selectors)
        {
            try
            {
                await page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
                {
                    Timeout = 4_000,
                    State = WaitForSelectorState.Attached
                });

                return;
            }
            catch (TimeoutException)
            {
            }
            catch (PlaywrightException)
            {
            }
        }
    }
}
