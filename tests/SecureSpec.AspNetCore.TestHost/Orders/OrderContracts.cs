using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace SecureSpec.AspNetCore.TestHost.Orders;

public enum OrderStatus
{
    Pending,
    Processing,
    Packed,
    Shipped,
    Delivered,
    Cancelled,
    Refunded
}

public sealed class Money
{
    private decimal _amount;

    public string Currency { get; init; } = "USD";

    public decimal Amount
    {
        get => _amount;
        init => _amount = decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}

public sealed class OrderItem
{
    public Guid ItemId { get; init; }

    public string Sku { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public Money UnitPrice { get; init; } = new();

    public Money LineTotal { get; init; } = new();

    public Dictionary<string, string>? Customizations { get; init; }
}

public sealed class CustomerProfile
{
    public string Id { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? LoyaltyTier { get; init; }

    public Dictionary<string, string>? Attributes { get; init; }
}

public sealed class Address
{
    public string Line1 { get; init; } = string.Empty;

    public string? Line2 { get; init; }

    public string City { get; init; } = string.Empty;

    public string Region { get; init; } = string.Empty;

    public string PostalCode { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;
}

public sealed record CardPaymentDetails
{
    public string Last4 { get; init; } = string.Empty;

    public string Network { get; init; } = string.Empty;

    public int ExpMonth { get; init; }

    public int ExpYear { get; init; }

    public bool Tokenized { get; init; }

    public Address? BillingAddress { get; init; }
}

public sealed class PaymentInstruction
{
    public string Method { get; init; } = string.Empty;

    public string? Reference { get; init; }

    public bool CaptureImmediately { get; init; } = true;

    public CardPaymentDetails? Card { get; init; }

    public Dictionary<string, string>? Metadata { get; init; }
}

public sealed record PaymentSummary
{
    public string Method { get; init; } = string.Empty;

    public string Reference { get; init; } = string.Empty;

    public bool CapturePending { get; init; }

    public decimal AmountAuthorized { get; init; }

    public decimal AmountCaptured { get; init; }

    public string Currency { get; init; } = "USD";

    public DateTimeOffset AuthorizedAt { get; init; }

    public DateTimeOffset? CapturedAt { get; init; }

    public CardPaymentDetails? Card { get; init; }

    public Dictionary<string, string>? Metadata { get; init; }
}

public sealed record OrderAuditEntry(OrderStatus Status, string Message, DateTimeOffset OccurredAt, string? Details);

public sealed class Order
{
    public Guid Id { get; set; }

    public string? ClientReference { get; set; }

    public OrderStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public CustomerProfile Customer { get; set; } = new();

    public Address ShippingAddress { get; set; } = new();

    public Address? BillingAddress { get; set; }

    public Collection<OrderItem> Items { get; set; } = new();

    public PaymentSummary Payment { get; set; } = new();

    public Money Subtotal { get; set; } = new();

    public Money Tax { get; set; } = new();

    public Money Total { get; set; } = new();

    public string? Notes { get; set; }

    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Collection<OrderAuditEntry> AuditTrail { get; set; } = new();
}

public sealed class CreateOrderItemRequest
{
    public string Sku { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public Money UnitPrice { get; init; } = new();

    public Dictionary<string, string>? Customizations { get; init; }
}

public sealed class CreateOrderRequest
{
    public string? ClientReference { get; init; }

    public CustomerProfile Customer { get; init; } = new();

    public Address ShippingAddress { get; init; } = new();

    public Address? BillingAddress { get; init; }

    public Collection<CreateOrderItemRequest> Items { get; init; } = new();

    public PaymentInstruction Payment { get; init; } = new();

    public string? Notes { get; init; }

    public Dictionary<string, string>? Metadata { get; init; }
}

public sealed class UpdateOrderStatusRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderStatus Status { get; init; }

    public string? Reason { get; init; }

    public bool? CapturePayment { get; init; }
}

public sealed class OrderMetadataPatch
{
    public Dictionary<string, string?>? Changes { get; init; }
}

public sealed class OrderQueryOptions
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderStatus? Status { get; init; }

    public int? Limit { get; init; }
}

public sealed class OrderListResponse
{
    public OrderStatus? StatusFilter { get; init; }

    public int Total { get; init; }

    public IReadOnlyList<Order> Items { get; init; } = Array.Empty<Order>();
}
