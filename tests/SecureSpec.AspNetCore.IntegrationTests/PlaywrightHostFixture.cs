using Microsoft.AspNetCore.Builder;

namespace SecureSpec.AspNetCore.IntegrationTests;

/// <summary>
/// Spins up the SecureSpec test host and Chromium browser for UI validation.
/// </summary>
public sealed class PlaywrightHostFixture : IAsyncLifetime
{
    private WebApplication? _app;
    private IBrowserContext? _context;
    private readonly List<string> _consoleErrors = new();

    public Uri BaseAddress { get; private set; } = null!;
    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;
    public IPage Page { get; private set; } = null!;
    public IReadOnlyList<string> ConsoleErrors => _consoleErrors;

    public async Task InitializeAsync()
    {
        _app = SecureSpecIntegrationApplication.Build();
        _app.Urls.Clear();
        _app.Urls.Add("http://127.0.0.1:0");
        await _app.StartAsync().ConfigureAwait(false);

        var httpUrl = _app.Urls
            .Select(static url => new Uri(url, UriKind.Absolute))
            .First(static uri => uri.Scheme == Uri.UriSchemeHttp);
        BaseAddress = httpUrl;

        Playwright = await Microsoft.Playwright.Playwright.CreateAsync().ConfigureAwait(false);
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = IsHeadless(),
            Channel = null
        }).ConfigureAwait(false);

        _context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseAddress.ToString()
        }).ConfigureAwait(false);

        Page = await _context.NewPageAsync().ConfigureAwait(false);
        Page.Console += (_, message) =>
        {
            if (string.Equals(message.Type, "error", StringComparison.OrdinalIgnoreCase))
            {
                _consoleErrors.Add(message.Text);
            }
        };
    }

    public async Task DisposeAsync()
    {
        if (Page is not null && !Page.IsClosed)
        {
            await Page.CloseAsync().ConfigureAwait(false);
        }

        if (_context is not null)
        {
            await _context.CloseAsync().ConfigureAwait(false);
        }

        if (Browser is not null)
        {
            await Browser.CloseAsync().ConfigureAwait(false);
        }

        Playwright?.Dispose();

        if (_app is not null)
        {
            await _app.StopAsync().ConfigureAwait(false);
            await _app.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static bool IsHeadless()
    {
        var headed = Environment.GetEnvironmentVariable("SECURESPEC_PLAYWRIGHT_HEADED");
        if (string.IsNullOrWhiteSpace(headed))
        {
            return true;
        }

        return !(string.Equals(headed, "1", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(headed, "true", StringComparison.OrdinalIgnoreCase));
    }
}
