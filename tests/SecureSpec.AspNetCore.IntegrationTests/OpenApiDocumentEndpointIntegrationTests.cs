using System.Net;
using System.Text.Json;

namespace SecureSpec.AspNetCore.IntegrationTests;

public class OpenApiDocumentEndpointIntegrationTests : IClassFixture<SecureSpecTestHostFactory>
{
    private readonly HttpClient _client;

    public OpenApiDocumentEndpointIntegrationTests(SecureSpecTestHostFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task OpenApiJson_RequestSetsNoCacheHeaders()
    {
        var response = await _client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());

        Assert.True(TryGetHeaderValue(response, "Cache-Control", out var cacheText));
        Assert.Contains("no-cache", cacheText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no-store", cacheText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("must-revalidate", cacheText, StringComparison.OrdinalIgnoreCase);

        Assert.True(TryGetHeaderValue(response, "Pragma", out var pragmaText));
        Assert.Contains("no-cache", pragmaText, StringComparison.OrdinalIgnoreCase);

        Assert.True(TryGetHeaderValue(response, "Expires", out var expiresText));
        Assert.Contains("0", expiresText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OpenApiDocument_MissingDocumentReturnsNotFound()
    {
        var response = await _client.GetAsync(new Uri("/openapi/missing.json", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("Document not found", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OpenApiDocument_UnsupportedFormatFallsBackToNotFound()
    {
        var response = await _client.GetAsync(new Uri("/openapi/v1.txt", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiDocument_IncludesOrdersApiDefinition()
    {
        var response = await _client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        response.EnsureSuccessStatusCode();
        await using var contentStream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(contentStream);

        var root = document.RootElement;
        Assert.True(root.TryGetProperty("paths", out var paths), "Expected OpenAPI document to contain paths section.");
        var pathNames = paths.EnumerateObject().Select(p => p.Name).ToArray();
        Assert.Contains("/orders", pathNames);
        Assert.Contains("/orders/{orderId}", pathNames);
        Assert.Contains("/orders/{orderId}/status", pathNames);
        Assert.Contains("/orders/{orderId}/metadata", pathNames);

        Assert.True(root.TryGetProperty("components", out var components), "Expected OpenAPI document to contain components section.");
        Assert.True(components.TryGetProperty("schemas", out var schemas), "Expected OpenAPI document to contain schemas section.");
        Assert.True(schemas.TryGetProperty("Order", out _), "Expected Order schema to be defined.");
        Assert.True(schemas.TryGetProperty("CreateOrderRequest", out _), "Expected CreateOrderRequest schema to be defined.");
        Assert.True(schemas.TryGetProperty("OrderMetadataPatch", out _), "Expected OrderMetadataPatch schema to be defined.");
    }

    private static bool TryGetHeaderValue(HttpResponseMessage response, string headerName, out string value)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentException.ThrowIfNullOrWhiteSpace(headerName);

        if (response.Headers.TryGetValues(headerName, out var values))
        {
            value = string.Join(",", values);
            return true;
        }

        if (response.Content.Headers.TryGetValues(headerName, out values))
        {
            value = string.Join(",", values);
            return true;
        }

        value = string.Empty;
        return false;
    }
}
