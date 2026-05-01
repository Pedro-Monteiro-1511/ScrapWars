using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Playwright;
using ScrapWars.Contracts.Events;

namespace ScrapWars.Scraper.Worker.Scraping;

public class IdealistaSiteScraper : ISiteScraper
{
    private readonly PlaywrightBrowserProvider _browserProvider;

    public IdealistaSiteScraper(PlaywrightBrowserProvider browserProvider)
    {
        _browserProvider = browserProvider;
    }

    public string SiteName => "idealista.pt";

    public bool CanHandle(Uri productUri)
    {
        return productUri.Host.Contains("idealista.pt", StringComparison.OrdinalIgnoreCase);
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

        await WaitForListingContentAsync(page);
        await page.WaitForTimeoutAsync(750);

        var priceFromPage = await TryGetPriceFromPageAsync(page);
        var previousPriceFromPage = await TryGetPreviousPriceFromPageAsync(page);
        var html = await page.ContentAsync();
        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(html, cancellationToken);

        var businessType = DetectBusinessType(document);
        var price = priceFromPage
            ?? TryGetPriceFromStructuredData(document)
            ?? TryGetPriceFromSelectors(document)
            ?? throw new InvalidOperationException("Could not extract the current listing price from idealista.pt.");
        var previousPrice = previousPriceFromPage ?? TryGetPreviousPriceFromSelectors(document);

        return new ScrapedProductResult
        {
            SiteName = SiteName,
            BusinessType = businessType,
            CurrentPrice = price,
            DiscountPercentage = CalculateDiscountPercentage(price, previousPrice),
            Currency = DetectCurrency(document),
            CapturedAtUtc = DateTime.UtcNow
        };
    }

    private static ListingBusinessType DetectBusinessType(IDocument document)
    {
        var textCandidates = new[]
        {
            document.QuerySelector("meta[property='og:title']")?.GetAttribute("content"),
            document.QuerySelector("meta[name='description']")?.GetAttribute("content"),
            document.Title,
            document.QuerySelector("h1")?.TextContent,
            document.QuerySelector("[class*='title']")?.TextContent
        };

        foreach (var candidate in textCandidates.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            var normalizedCandidate = NormalizeText(candidate!);

            if (normalizedCandidate.Contains("arrendamento", StringComparison.Ordinal) ||
                normalizedCandidate.Contains("alugar", StringComparison.Ordinal))
            {
                return ListingBusinessType.Rent;
            }

            if (normalizedCandidate.Contains("venda", StringComparison.Ordinal) ||
                normalizedCandidate.Contains("a venda", StringComparison.Ordinal))
            {
                return ListingBusinessType.Sale;
            }
        }

        return ListingBusinessType.Unknown;
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

    private static decimal? TryGetPreviousPriceFromSelectors(IDocument document)
    {
        var selectors = new[]
        {
            ".pricedown_price",
            ".pricedown .pricedown_price"
        };

        foreach (var selector in selectors)
        {
            foreach (var node in document.QuerySelectorAll(selector))
            {
                if (TryParsePrice(node.TextContent, out var price))
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
                if (property.NameEquals("price") && TryParsePrice(property.Value.ToString(), out var parsedPrice))
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

    private static async Task<decimal?> TryGetPreviousPriceFromPageAsync(IPage page)
    {
        var selectors = new[]
        {
            ".pricedown_price",
            ".pricedown .pricedown_price"
        };

        foreach (var selector in selectors)
        {
            try
            {
                var locator = page.Locator(selector).First;

                if (await locator.CountAsync() == 0)
                {
                    continue;
                }

                var text = await locator.TextContentAsync();

                if (TryParsePrice(text, out var price))
                {
                    return price;
                }
            }
            catch (PlaywrightException)
            {
            }
        }

        return null;
    }

    private static decimal? TryGetPriceFromSelectors(IDocument document)
    {
        var selectors = new[]
        {
            ".info-data-price .txt-bold",
            ".info-data-price",
            "[itemprop='price']",
            "meta[property='product:price:amount']",
            ".info-data-price",
            ".price",
            ".txt-big",
            "[class*='price']"
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

    private static async Task<decimal?> TryGetPriceFromPageAsync(IPage page)
    {
        var selectors = new[]
        {
            ".info-data-price .txt-bold",
            ".info-data-price",
            "[itemprop='price']",
            "[class*='price']"
        };

        foreach (var selector in selectors)
        {
            try
            {
                var locator = page.Locator(selector).First;

                if (await locator.CountAsync() == 0)
                {
                    continue;
                }

                var text = await locator.TextContentAsync();

                if (TryParsePrice(text, out var price))
                {
                    return price;
                }
            }
            catch (PlaywrightException)
            {
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
                NormalizeText(candidate).Contains("euros", StringComparison.Ordinal))
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

        var numeric = Regex.Match(rawValue, @"\d[\d\.\,\s]*").Value;

        if (string.IsNullOrWhiteSpace(numeric))
        {
            return false;
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

    private static string NormalizeText(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static async Task WaitForListingContentAsync(IPage page)
    {
        var selectors = new[]
        {
            ".info-data-price .txt-bold",
            ".info-data-price",
            "[itemprop='price']",
            "[class*='price']",
            "h1"
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
