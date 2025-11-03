namespace SecureSpec.AspNetCore.IntegrationTests;

[Collection(PlaywrightCollection.Name)]
public sealed class SecureSpecUiTests
{
    private readonly PlaywrightHostFixture _fixture;

    public SecureSpecUiTests(PlaywrightHostFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _fixture = fixture;
    }

    [Fact]
    public async Task SecureSpecUi_LoadsLayoutAssetsAndOperations()
    {
        var page = _fixture.Page;

        await page.GotoAsync("/securespec", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        await Microsoft.Playwright.Assertions.Expect(page.GetByTestId("securespec-root")).ToBeVisibleAsync();
        var navigation = page.GetByTestId("securespec-navigation");
        await Microsoft.Playwright.Assertions.Expect(page.GetByTestId("securespec-header")).ToContainTextAsync("SecureSpec Integration API");
        await Microsoft.Playwright.Assertions.Expect(page.Locator("link[rel='stylesheet'][href='assets/styles.css']")).ToHaveCountAsync(1);
        await Microsoft.Playwright.Assertions.Expect(page.Locator("script[type=\"module\"][src='assets/app.js']")).ToHaveCountAsync(1);
        await Microsoft.Playwright.Assertions.Expect(navigation).ToBeVisibleAsync();
        await Microsoft.Playwright.Assertions.Expect(page.Locator("#operation-filter")).ToBeVisibleAsync();

        await Microsoft.Playwright.Assertions.Expect(page.GetByTestId("securespec-content")).ToContainTextAsync("Get weather forecast");

        Assert.Empty(_fixture.ConsoleErrors);
    }
}
