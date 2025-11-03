using System.Net;

namespace SecureSpec.AspNetCore.IntegrationTests;

/// <summary>
/// Verifies asset caching behavior when public caching is enabled.
/// </summary>
public class AssetCachingConfigurationIntegrationTests : IClassFixture<PublicCacheTestHostFactory>
{
    private readonly HttpClient _client;

    public AssetCachingConfigurationIntegrationTests(PublicCacheTestHostFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AssetRequest_WithPublicCaching_EmitsPublicCacheControlWithoutEtag()
    {
        var response = await _client.GetAsync(new Uri("/assets/styles.css", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cacheControl = response.Headers.CacheControl;
        Assert.NotNull(cacheControl);
        Assert.Contains("public", cacheControl!.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("must-revalidate", cacheControl.ToString(), StringComparison.Ordinal);
        Assert.False(response.Headers.Contains("ETag"));
    }
}
