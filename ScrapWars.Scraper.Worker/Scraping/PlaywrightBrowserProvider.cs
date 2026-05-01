using Microsoft.Playwright;

namespace ScrapWars.Scraper.Worker.Scraping;

public class PlaywrightBrowserProvider : IAsyncDisposable
{
    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async Task<IBrowser> GetBrowserAsync(CancellationToken cancellationToken = default)
    {
        if (_browser is not null)
        {
            return _browser;
        }

        await _initializationLock.WaitAsync(cancellationToken);

        try
        {
            if (_browser is not null)
            {
                return _browser;
            }

            _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args =
                [
                    "--disable-blink-features=AutomationControlled",
                    "--no-sandbox"
                ]
            });

            return _browser;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();
        _initializationLock.Dispose();
    }
}
