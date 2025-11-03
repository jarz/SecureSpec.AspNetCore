using System.Net;
using System.Text.Json;

namespace SecureSpec.AspNetCore.IntegrationTests;

/// <summary>
/// Validates metadata emitted by the document generation integration endpoints.
/// </summary>
public class DocumentGenerationMetadataIntegrationTests : IClassFixture<SecureSpecTestHostFactory>
{
    private readonly HttpClient _client;

    public DocumentGenerationMetadataIntegrationTests(SecureSpecTestHostFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DocumentGeneration_SuccessfulResponseSetsFallbackHeaderFalse()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/integration/documents/v1?format=json");
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("false", Assert.Single(response.Headers.GetValues("X-SecureSpec-Fallback")));
    }

    [Fact]
    public async Task DocumentGeneration_DifferentFormatsEmitDistinctIntegrityMarkers()
    {
        using var jsonRequest = new HttpRequestMessage(HttpMethod.Post, "/integration/documents/v1?format=json");
        using var jsonResponse = await _client.SendAsync(jsonRequest);

        Assert.Equal(HttpStatusCode.OK, jsonResponse.StatusCode);
        var jsonEtag = jsonResponse.Headers.ETag?.Tag;
        var jsonSri = Assert.Single(jsonResponse.Headers.GetValues("X-SecureSpec-Sri"));
        var jsonCacheKey = Assert.Single(jsonResponse.Headers.GetValues("X-SecureSpec-Cache-Key"));

        using var yamlRequest = new HttpRequestMessage(HttpMethod.Post, "/integration/documents/v1?format=yaml");
         using var yamlResponse = await _client.SendAsync(yamlRequest);

        Assert.Equal(HttpStatusCode.OK, yamlResponse.StatusCode);
        var yamlEtag = yamlResponse.Headers.ETag?.Tag;
        var yamlSri = Assert.Single(yamlResponse.Headers.GetValues("X-SecureSpec-Sri"));
        var yamlCacheKey = Assert.Single(yamlResponse.Headers.GetValues("X-SecureSpec-Cache-Key"));

        Assert.False(string.Equals(jsonEtag, yamlEtag, StringComparison.Ordinal));
        Assert.False(string.Equals(jsonSri, yamlSri, StringComparison.Ordinal));
        Assert.Equal("v1.json", jsonCacheKey);
        Assert.Equal("v1.yaml", yamlCacheKey);
    }

    [Fact]
    public async Task DocumentCache_InvalidateAllClearsEntriesForAllFormats()
    {
        using var jsonRequest = new HttpRequestMessage(HttpMethod.Post, "/integration/documents/v1?format=json");
        using var yamlRequest = new HttpRequestMessage(HttpMethod.Post, "/integration/documents/v1?format=yaml");
        using var invalidateRequest = new HttpRequestMessage(HttpMethod.Post, "/integration/documents/invalidate-all");

        await _client.SendAsync(jsonRequest);
        await _client.SendAsync(yamlRequest);
        using var invalidateResponse = await _client.SendAsync(invalidateRequest);

        Assert.Equal(HttpStatusCode.OK, invalidateResponse.StatusCode);
        using var invalidateJson = JsonDocument.Parse(await invalidateResponse.Content.ReadAsStringAsync());
        Assert.Equal(0, invalidateJson.RootElement.GetProperty("remaining").GetInt32());

        var jsonCacheResponse = await _client.GetAsync(new Uri("/integration/documents/v1/cache?format=json", UriKind.Relative));
        using var jsonCache = JsonDocument.Parse(await jsonCacheResponse.Content.ReadAsStringAsync());
        Assert.False(jsonCache.RootElement.GetProperty("found").GetBoolean());

        var yamlCacheResponse = await _client.GetAsync(new Uri("/integration/documents/v1/cache?format=yaml", UriKind.Relative));
        using var yamlCache = JsonDocument.Parse(await yamlCacheResponse.Content.ReadAsStringAsync());
        Assert.False(yamlCache.RootElement.GetProperty("found").GetBoolean());
    }

    [Fact]
    public async Task DocumentGeneration_PreservesConfiguredSecurityMetadata()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/integration/documents/v1?format=json");
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("4", Assert.Single(response.Headers.GetValues("X-SecureSpec-Security-Scheme-Count")));

        var payload = await response.Content.ReadAsStringAsync();
        using var documentJson = JsonDocument.Parse(payload);
        var root = documentJson.RootElement;

        Assert.Equal("SecureSpec Integration API", root.GetProperty("info").GetProperty("title").GetString());
        Assert.Equal("SecureSpec integration testing surface", root.GetProperty("info").GetProperty("description").GetString());

        Assert.True(root.TryGetProperty("security", out var securityArray), "Document missing security requirements");
        AssertSecurityRequirement(securityArray, "bearerAuth");
        AssertSecurityRequirement(securityArray, "apiKeyHeader");
        AssertSecurityRequirement(securityArray, "oauth2");
        AssertSecurityRequirement(securityArray, "mutualTLS");

        var weatherPath = root.GetProperty("paths").GetProperty("/weatherforecast");
        Assert.Equal("GetWeatherForecast", weatherPath.GetProperty("get").GetProperty("operationId").GetString());
    }

    private static void AssertSecurityRequirement(JsonElement securityArray, string schemeName)
    {
        Assert.Contains(securityArray.EnumerateArray(), element => element.TryGetProperty(schemeName, out _));
    }
}
