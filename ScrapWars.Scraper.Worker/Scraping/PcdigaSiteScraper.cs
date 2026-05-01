using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Playwright;
using ScrapWars.Contracts.Events;

namespace ScrapWars.Scraper.Worker.Scraping;

public class PcdigaSiteScraper : ISiteScraper
{
    private readonly PlaywrightBrowserProvider _browserProvider;

    public PcdigaSiteScraper(PlaywrightBrowserProvider browserProvider)
    {
        _browserProvider = browserProvider;
    }

    public string SiteName => "pcdiga.com";

    public bool CanHandle(Uri productUri)
    {
        return productUri.Host.Contains("pcdiga.com", StringComparison.OrdinalIgnoreCase);
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

        await WaitForProductContentAsync(page);
        await page.WaitForTimeoutAsync(750);

        var priceFromPage = await TryGetPriceFromPageAsync(page);
        var previousPriceFromPage = await TryGetPreviousPriceFromPageAsync(page);
        var html = await page.ContentAsync();
        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(html, cancellationToken);

        var price = priceFromPage
            ?? TryGetPriceFromStructuredData(document)
            ?? TryGetPriceFromSelectors(document)
            ?? throw new InvalidOperationException("Could not extract the current product price from pcdiga.com.");
        var previousPrice = previousPriceFromPage ?? TryGetPreviousPriceFromSelectors(document);

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
                var price = FindProductPrice(jsonDocument.RootElement);

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
            ".sticky .line-through",
            ".pvpr-lh .line-through",
            "p.line-through"
        };

        foreach (var selector in selectors)
        {
            foreach (var node in document.QuerySelectorAll(selector))
            {
                var rawValue = node.TextContent;

                if (TryParsePrice(rawValue, out var price))
                {
                    return price;
                }
            }
        }

        return null;
    }

    private static decimal? FindProductPrice(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (IsProductNode(element) && TryGetOfferPrice(element, out var price))
            {
                return price;
            }

            foreach (var property in element.EnumerateObject())
            {
                var nestedPrice = FindProductPrice(property.Value);

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
                var nestedPrice = FindProductPrice(item);

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
            ".sticky .line-through",
            ".pvpr-lh .line-through",
            "p.line-through"
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

    private static bool IsProductNode(JsonElement element)
    {
        if (!element.TryGetProperty("@type", out var typeElement))
        {
            return false;
        }

        return typeElement.ValueKind switch
        {
            JsonValueKind.String => typeElement.GetString()?.Contains("Product", StringComparison.OrdinalIgnoreCase) == true,
            JsonValueKind.Array => typeElement.EnumerateArray()
                .Any(item => item.ValueKind == JsonValueKind.String &&
                             item.GetString()?.Contains("Product", StringComparison.OrdinalIgnoreCase) == true),
            _ => false
        };
    }

    private static bool TryGetOfferPrice(JsonElement productElement, out decimal price)
    {
        price = 0;

        if (!productElement.TryGetProperty("offers", out var offersElement))
        {
            return false;
        }

        return TryGetPriceFromOfferElement(offersElement, out price);
    }

    private static bool TryGetPriceFromOfferElement(JsonElement element, out decimal price)
    {
        price = 0;

        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("price", out var priceElement) &&
                TryParsePrice(priceElement.ToString(), out price))
            {
                return true;
            }

            if (element.TryGetProperty("lowPrice", out var lowPriceElement) &&
                TryParsePrice(lowPriceElement.ToString(), out price))
            {
                return true;
            }
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (TryGetPriceFromOfferElement(item, out price))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static decimal? TryGetPriceFromSelectors(IDocument document)
    {
        var selectors = new[]
        {
            "meta[property='product:price:amount']",
            ".sticky .text-primary.text-2xl",
            ".sticky .text-primary.md\\:text-3xl",
            "main h1 + div .text-primary",
            ".product-content .text-primary.text-2xl"
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
            "meta[property='product:price:amount']",
            ".sticky .text-primary.text-2xl",
            ".sticky .text-primary.md\\:text-3xl",
            ".product-content .text-primary.text-2xl"
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

                var rawValue = await locator.GetAttributeAsync("content") ?? await locator.TextContentAsync();

                if (TryParsePrice(rawValue, out var price))
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
            TryGetCurrencyFromStructuredData(document),
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

    private static string? TryGetCurrencyFromStructuredData(IDocument document)
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
                var currency = FindCurrency(jsonDocument.RootElement);

                if (!string.IsNullOrWhiteSpace(currency))
                {
                    return currency;
                }
            }
            catch (JsonException)
            {
            }
        }

        return null;
    }

    private static string? FindCurrency(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("priceCurrency", out var currencyElement) &&
                currencyElement.ValueKind == JsonValueKind.String)
            {
                return currencyElement.GetString();
            }

            foreach (var property in element.EnumerateObject())
            {
                var nestedCurrency = FindCurrency(property.Value);

                if (!string.IsNullOrWhiteSpace(nestedCurrency))
                {
                    return nestedCurrency;
                }
            }
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nestedCurrency = FindCurrency(item);

                if (!string.IsNullOrWhiteSpace(nestedCurrency))
                {
                    return nestedCurrency;
                }
            }
        }

        return null;
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

    private static async Task WaitForProductContentAsync(IPage page)
    {
        var selectors = new[]
        {
            "script[type='application/ld+json']",
            ".sticky .text-primary.text-2xl",
            ".product-content .text-primary.text-2xl",
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
