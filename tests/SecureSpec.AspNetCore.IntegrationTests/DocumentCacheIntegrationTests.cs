using System.Net;
using System.Text.Json;

namespace SecureSpec.AspNetCore.IntegrationTests;

/// <summary>
/// Exercises document generation and caching integration endpoints to increase coverage of document caching components.
/// </summary>
public class DocumentCacheIntegrationTests : IClassFixture<SecureSpecTestHostFactory>
{
    private readonly HttpClient _client;

    public DocumentCacheIntegrationTests(SecureSpecTestHostFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DocumentGeneration_CachesSerializedDocument()
    {
        using var generateRequest = new HttpRequestMessage(HttpMethod.Post, "/integration/documents/v1?format=json");
        var response = await _client.SendAsync(generateRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());

        var cacheKey = Assert.Single(response.Headers.GetValues("X-SecureSpec-Cache-Key"));
        var sri = Assert.Single(response.Headers.GetValues("X-SecureSpec-Sri"));
        Assert.False(string.IsNullOrWhiteSpace(sri));
        Assert.StartsWith("\"sha256:", response.Headers.ETag?.Tag, StringComparison.Ordinal);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("GetWeatherForecast", body, StringComparison.Ordinal);

        var cacheResponse = await _client.GetAsync(new Uri("/integration/documents/v1/cache?format=json", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, cacheResponse.StatusCode);

        using var cacheJson = JsonDocument.Parse(await cacheResponse.Content.ReadAsStringAsync());
        var root = cacheJson.RootElement;
        Assert.True(root.GetProperty("found").GetBoolean());
        Assert.Equal(cacheKey, root.GetProperty("cacheKey").GetString());
        Assert.True(root.GetProperty("length").GetInt32() > 0);
    }

    [Fact]
    public async Task DocumentGeneration_WithSimulatedLimit_ProducesFallbackDocument()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/integration/documents/v1?simulateLimit=true");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("true", Assert.Single(response.Headers.GetValues("X-SecureSpec-Fallback")));

        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("Document Generation Failed", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DocumentCache_ExpiresInvalidatesAndEvictsEntries()
    {
        using var generateRequest = new HttpRequestMessage(HttpMethod.Post, "/integration/documents/v1?format=json&expireSeconds=0.05");
        var response = await _client.SendAsync(generateRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await Task.Delay(TimeSpan.FromMilliseconds(120));

        var cacheCheck = await _client.GetAsync(new Uri("/integration/documents/v1/cache?format=json", UriKind.Relative));
        using var cacheJson = JsonDocument.Parse(await cacheCheck.Content.ReadAsStringAsync());
        Assert.False(cacheJson.RootElement.GetProperty("found").GetBoolean());

        using var evictRequest = new HttpRequestMessage(HttpMethod.Post, "/integration/documents/evict");
        var evictResponse = await _client.SendAsync(evictRequest);
        Assert.Equal(HttpStatusCode.OK, evictResponse.StatusCode);

        using var evictJson = JsonDocument.Parse(await evictResponse.Content.ReadAsStringAsync());
        Assert.True(evictJson.RootElement.GetProperty("evicted").GetInt32() >= 0);

        using var regenerateRequest = new HttpRequestMessage(HttpMethod.Post, "/integration/documents/v1?format=json");
        var regenerateResponse = await _client.SendAsync(regenerateRequest);
        Assert.Equal(HttpStatusCode.OK, regenerateResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync(new Uri("/integration/documents/v1?format=json", UriKind.Relative));
        using var deleteJson = JsonDocument.Parse(await deleteResponse.Content.ReadAsStringAsync());
        Assert.True(deleteJson.RootElement.GetProperty("removed").GetBoolean());

        using var purgeRequest = new HttpRequestMessage(HttpMethod.Post, "/integration/documents/invalidate-all");
        var purgeResponse = await _client.SendAsync(purgeRequest);
        using var purgeJson = JsonDocument.Parse(await purgeResponse.Content.ReadAsStringAsync());
        Assert.Equal(0, purgeJson.RootElement.GetProperty("remaining").GetInt32());
    }
}
