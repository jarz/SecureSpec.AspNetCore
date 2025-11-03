using System.Net;

namespace SecureSpec.AspNetCore.IntegrationTests;

/// <summary>
/// Ensures the generated OpenAPI documents are stable and match the committed fixtures.
/// </summary>
public class SchemaGenerationIntegrationTests : IClassFixture<SecureSpecTestHostFactory>
{
    private readonly HttpClient _client;

    public SchemaGenerationIntegrationTests(SecureSpecTestHostFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task OpenApiJson_MatchesCanonicalFixture()
    {
        var response = await _client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        var canonical = SchemaCanonicalizer.CanonicalizeJson(payload);
        var expected = await FixtureStore.ReadAsync("v1.json");

        Assert.Equal(expected, canonical);
    }

    [Fact]
    public async Task OpenApiYaml_MatchesCanonicalFixture()
    {
        var response = await _client.GetAsync(new Uri("/openapi/v1.yaml", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        var canonical = SchemaCanonicalizer.CanonicalizeYaml(payload);
        var expected = await FixtureStore.ReadAsync("v1.yaml");

        Assert.Equal(expected, canonical);
    }
}
