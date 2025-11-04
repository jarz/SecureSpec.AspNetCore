using System.Net;

namespace SecureSpec.AspNetCore.IntegrationTests;

public class SecureSpecUiSecurityHeadersIntegrationTests : IClassFixture<SecureSpecTestHostFactory>
{
    private readonly HttpClient _client;

    public SecureSpecUiSecurityHeadersIntegrationTests(SecureSpecTestHostFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SecureSpecUi_IndexPageEmitsSecurityHeaders()
    {
        var response = await _client.GetAsync(new Uri("/securespec", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());

        Assert.True(response.Headers.TryGetValues("Content-Security-Policy", out var cspValues));
        Assert.Contains("default-src 'none'", string.Join(' ', cspValues), StringComparison.Ordinal);

        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
        Assert.Equal("no-referrer", Assert.Single(response.Headers.GetValues("Referrer-Policy")));

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("SecureSpec Integration API", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SecureSpecUi_AssetsServedUnderRoutePrefixIncludeCacheMetadata()
    {
        var response = await _client.GetAsync(new Uri("/securespec/assets/styles.css", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/css; charset=utf-8", response.Content.Headers.ContentType?.ToString());

        var cacheControl = response.Headers.CacheControl;
        Assert.NotNull(cacheControl);
        Assert.Equal(TimeSpan.FromSeconds(600), cacheControl!.MaxAge);
        Assert.False(cacheControl.Public);
        Assert.True(cacheControl.Private);
        Assert.True(cacheControl.MustRevalidate);

        Assert.NotNull(response.Headers.ETag);
    }

    [Fact]
    public async Task SecureSpecUi_AssetLookupIsCaseInsensitive()
    {
        var response = await _client.GetAsync(new Uri("/SECURESPEC/ASSETS/APP.JS", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/javascript; charset=utf-8", response.Content.Headers.ContentType?.ToString());

        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("SECURESPEC", payload, StringComparison.OrdinalIgnoreCase);
    }
}
