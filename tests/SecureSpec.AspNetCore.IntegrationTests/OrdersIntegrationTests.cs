using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SecureSpec.AspNetCore.IntegrationTests;

public class OrdersIntegrationTests : IClassFixture<SecureSpecTestHostFactory>
{
    private readonly HttpClient _client;

    public OrdersIntegrationTests(SecureSpecTestHostFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateOrder_FullLifecycle_Succeeds()
    {
        var createRequest = new
        {
            clientReference = "WEB-555",
            customer = new
            {
                id = "cust-555",
                email = "ada@example.com",
                firstName = "Ada",
                lastName = "Lovelace",
                loyaltyTier = "platinum"
            },
            shippingAddress = new
            {
                line1 = "123 Binary Rd",
                city = "London",
                region = "LDN",
                postalCode = "SW1A 1AA",
                country = "UK"
            },
            billingAddress = new
            {
                line1 = "123 Binary Rd",
                city = "London",
                region = "LDN",
                postalCode = "SW1A 1AA",
                country = "UK"
            },
            items = new[]
            {
                new
                {
                    sku = "COMP-001",
                    name = "Compute Module",
                    quantity = 2,
                    unitPrice = new { currency = "USD", amount = 349.99m },
                    customizations = new Dictionary<string, string>
                    {
                        ["finish"] = "anodized",
                        ["bundle"] = "pro"
                    }
                },
                new
                {
                    sku = "CLOUD-CREDITS",
                    name = "Cloud Credits",
                    quantity = 1,
                    unitPrice = new { currency = "USD", amount = 1200.00m },
                    customizations = new Dictionary<string, string>()
                }
            },
            payment = new
            {
                method = "card",
                captureImmediately = false,
                card = new
                {
                    last4 = "1881",
                    network = "visa",
                    expMonth = 10,
                    expYear = DateTime.UtcNow.Year + 4,
                    tokenized = true,
                    billingAddress = new
                    {
                        line1 = "123 Binary Rd",
                        city = "London",
                        region = "LDN",
                        postalCode = "SW1A 1AA",
                        country = "UK"
                    }
                },
                metadata = new Dictionary<string, string>
                {
                    ["channel"] = "web",
                    ["riskScore"] = "low"
                }
            },
            metadata = new Dictionary<string, string>
            {
                ["priority"] = "high"
            },
            notes = "Ship with signature required"
        };

        var createResponse = await _client.PostAsJsonAsync(new Uri("/orders", UriKind.Relative), createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createResponse.Headers.Location);

        var createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = createdOrder.GetProperty("id").GetGuid();
        Assert.Equal("Pending", createdOrder.GetProperty("status").GetString());
        Assert.Equal("USD", createdOrder.GetProperty("total").GetProperty("currency").GetString());
        Assert.Equal(2, createdOrder.GetProperty("items").GetArrayLength());
        Assert.True(createdOrder.GetProperty("payment").GetProperty("capturePending").GetBoolean());

        var getResponse = await _client.GetAsync(new Uri($"/orders/{orderId}", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetchedOrder = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(orderId, fetchedOrder.GetProperty("id").GetGuid());
        Assert.Equal("SHIP WITH SIGNATURE REQUIRED", fetchedOrder.GetProperty("notes").GetString()?.ToUpperInvariant());

        var statusResponse = await _client.PutAsJsonAsync(new Uri($"/orders/{orderId}/status", UriKind.Relative), new
        {
            status = "Processing",
            reason = "Picked and packed",
            capturePayment = true
        });

        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        var updatedOrder = await statusResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Processing", updatedOrder.GetProperty("status").GetString());
        Assert.False(updatedOrder.GetProperty("payment").GetProperty("capturePending").GetBoolean());
        Assert.Equal(updatedOrder.GetProperty("payment").GetProperty("amountAuthorized").GetDecimal(), updatedOrder.GetProperty("payment").GetProperty("amountCaptured").GetDecimal());

        var patchContent = JsonContent.Create(new
        {
            changes = new Dictionary<string, string?>
            {
                ["priority"] = "standard",
                ["internal"] = "Y123",
                ["obsolete"] = null
            }
        });

        using var patchRequest = new HttpRequestMessage(HttpMethod.Patch, new Uri($"/orders/{orderId}/metadata", UriKind.Relative))
        {
            Content = patchContent
        };

        var patchResponse = await _client.SendAsync(patchRequest);
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
        var patchedOrder = await patchResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("standard", patchedOrder.GetProperty("metadata").GetProperty("priority").GetString());
        Assert.Equal("Y123", patchedOrder.GetProperty("metadata").GetProperty("internal").GetString());
        Assert.False(patchedOrder.GetProperty("metadata").TryGetProperty("obsolete", out _));

        var deleteResponse = await _client.DeleteAsync(new Uri($"/orders/{orderId}", UriKind.Relative));
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var notFound = await _client.GetAsync(new Uri($"/orders/{orderId}", UriKind.Relative));
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task UpdateOrderStatus_InvalidTransition_ReturnsConflict()
    {
        var listResponse = await _client.GetAsync(new Uri("/orders?status=Delivered", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var payload = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var items = payload.GetProperty("items");
        Assert.NotEqual(0, items.GetArrayLength());

        var enumerator = items.EnumerateArray();
        Assert.True(enumerator.MoveNext());
        var orderId = enumerator.Current.GetProperty("id").GetGuid();
        var response = await _client.PutAsJsonAsync(new Uri($"/orders/{orderId}/status", UriKind.Relative), new
        {
            status = "Processing",
            reason = "Attempt to rewind"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Invalid status transition", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task CreateOrder_InvalidPayload_ReturnsValidationProblem()
    {
        var request = new
        {
            customer = new
            {
                id = "cust-001",
                email = "invalid-email"
            },
            shippingAddress = new
            {
                line1 = string.Empty,
                city = "",
                region = "",
                postalCode = "",
                country = "US"
            },
            items = Array.Empty<object>(),
            payment = new
            {
                method = string.Empty
            }
        };

        var response = await _client.PostAsJsonAsync(new Uri("/orders", UriKind.Relative), request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("customer.email", out _));
        Assert.True(errors.TryGetProperty("items", out _));
        Assert.True(errors.TryGetProperty("payment.method", out _));
    }
}
