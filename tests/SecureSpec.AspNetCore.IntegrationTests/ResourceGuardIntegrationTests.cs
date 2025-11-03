using System.Net;

namespace SecureSpec.AspNetCore.IntegrationTests;

/// <summary>
/// Covers resource guard edge cases exposed through the integration endpoints.
/// </summary>
public class ResourceGuardIntegrationTests : IClassFixture<ResourceGuardTestHostFactory>
{
    private readonly HttpClient _client;

    public ResourceGuardIntegrationTests(ResourceGuardTestHostFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DocumentGeneration_WithSimulatedDelay_TriggersTimeGuardFallback()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/integration/documents/v1?format=json&simulateDelayMs=250");
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("true", Assert.Single(response.Headers.GetValues("X-SecureSpec-Fallback")));

        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("Generation time exceeded limit", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DocumentGeneration_WithSimulatedMemory_TriggersMemoryGuardFallback()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/integration/documents/v1?format=json&simulateMemoryBytes=524288");
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("true", Assert.Single(response.Headers.GetValues("X-SecureSpec-Fallback")));

        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("Memory usage exceeded limit", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DocumentGeneration_RecoversAfterFallbackWithFreshCacheEntry()
    {
        using var fallbackRequest = new HttpRequestMessage(HttpMethod.Post, "/integration/documents/v1?format=json&simulateDelayMs=250");
        using var fallbackResponse = await _client.SendAsync(fallbackRequest);
        var fallbackEtag = fallbackResponse.Headers.ETag?.Tag;

        using var recoveryRequest = new HttpRequestMessage(HttpMethod.Post, "/integration/documents/v1?format=json");
        using var recoveryResponse = await _client.SendAsync(recoveryRequest);

        Assert.Equal(HttpStatusCode.OK, recoveryResponse.StatusCode);
        Assert.Equal("false", Assert.Single(recoveryResponse.Headers.GetValues("X-SecureSpec-Fallback")));
        Assert.NotEqual(fallbackEtag, recoveryResponse.Headers.ETag?.Tag);
    }
}
