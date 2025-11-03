using System.Net;

namespace SecureSpec.AspNetCore.IntegrationTests;

/// <summary>
/// Validates caching and integrity behavior for SecureSpec UI assets.
/// </summary>
public class AssetCachingIntegrationTests : IClassFixture<SecureSpecTestHostFactory>
{
    private readonly HttpClient _client;

    public AssetCachingIntegrationTests(SecureSpecTestHostFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AssetRequest_EmitsCacheHeadersAndSecurityPolicies()
    {
        var response = await _client.GetAsync(new Uri("/assets/styles.css", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/css; charset=utf-8", response.Content.Headers.ContentType?.ToString());

        var cacheControl = response.Headers.CacheControl;
        Assert.NotNull(cacheControl);
        Assert.Equal(TimeSpan.FromSeconds(600), cacheControl!.MaxAge);
        var cacheControlText = cacheControl.ToString();
        Assert.Contains("private", cacheControlText, StringComparison.Ordinal);
        Assert.Contains("must-revalidate", cacheControlText, StringComparison.Ordinal);

        Assert.NotNull(response.Headers.ETag);
        Assert.StartsWith("\"sha256:", response.Headers.ETag!.Tag, StringComparison.Ordinal);

        Assert.True(response.Headers.TryGetValues("Content-Security-Policy", out var cspValues));
        Assert.Contains("default-src 'none'", string.Concat(cspValues), StringComparison.Ordinal);

        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
    }

    [Fact]
    public async Task AssetRequest_MissingAsset_ReturnsNotFound()
    {
        var response = await _client.GetAsync(new Uri("/assets/missing.js", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Asset not found", body, StringComparison.Ordinal);
        Assert.False(response.Headers.Contains("ETag"));
    }

    [Fact]
    public async Task NonAssetRequest_BypassesCaching()
    {
        var response = await _client.GetAsync(new Uri("/swagger.json", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(response.Headers.Contains("Cache-Control"));
    }

    [Fact]
    public async Task AssetRequest_WithMatchingETag_ReturnsNotModified()
    {
        var initial = await _client.GetAsync(new Uri("/assets/app.js", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, initial.StatusCode);
        var etag = initial.Headers.ETag;
        Assert.NotNull(etag);

        using var followUp = new HttpRequestMessage(HttpMethod.Get, "/assets/app.js");
        followUp.Headers.IfNoneMatch.Add(etag!);

        var secondResponse = await _client.SendAsync(followUp);

        Assert.Equal(HttpStatusCode.NotModified, secondResponse.StatusCode);
        Assert.Equal(etag!.Tag, secondResponse.Headers.ETag?.Tag);
        Assert.Empty(await secondResponse.Content.ReadAsByteArrayAsync());
    }
}
