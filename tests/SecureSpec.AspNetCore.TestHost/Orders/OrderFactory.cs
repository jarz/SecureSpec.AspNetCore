using System.Collections.ObjectModel;

namespace SecureSpec.AspNetCore.TestHost.Orders;

internal static class OrderFactory
{
    private const decimal TaxRate = 0.0825m;

    public static Order Create(CreateOrderRequest request, Guid? orderId = null, DateTimeOffset? createdAt = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var items = BuildItems(request);
        var currency = items[0].UnitPrice.Currency;
        var subtotalAmount = items.Sum(item => item.LineTotal.Amount);
        var taxAmount = decimal.Round(subtotalAmount * TaxRate, 2, MidpointRounding.AwayFromZero);
        var totalAmount = subtotalAmount + taxAmount;

        var now = createdAt ?? DateTimeOffset.UtcNow;
        var identifier = orderId ?? Guid.NewGuid();

        var payment = BuildPayment(request.Payment, currency, totalAmount, now);

        var order = new Order
        {
            Id = identifier,
            ClientReference = request.ClientReference,
            Status = OrderStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            Customer = Clone(request.Customer),
            ShippingAddress = Clone(request.ShippingAddress),
            BillingAddress = request.BillingAddress is null ? null : Clone(request.BillingAddress),
            Items = items,
            Payment = payment,
            Subtotal = new Money { Currency = currency, Amount = subtotalAmount },
            Tax = new Money { Currency = currency, Amount = taxAmount },
            Total = new Money { Currency = currency, Amount = totalAmount },
            Notes = request.Notes,
            Metadata = request.Metadata is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(request.Metadata, StringComparer.OrdinalIgnoreCase),
            AuditTrail = new Collection<OrderAuditEntry>
            {
                new(OrderStatus.Pending, "Order created", now, request.Notes)
            }
        };

        EnsureMetadata(order);

        return order;
    }

    private static Collection<OrderItem> BuildItems(CreateOrderRequest request)
    {
        if (request.Items.Count == 0)
        {
            throw new InvalidOperationException("Order must contain at least one item.");
        }

        return new Collection<OrderItem>(request.Items.Select(item =>
        {
            var unitPrice = item.UnitPrice ?? throw new InvalidOperationException("Unit price is required.");

            return new OrderItem
            {
                ItemId = Guid.NewGuid(),
                Sku = item.Sku,
                Name = item.Name,
                Quantity = item.Quantity,
                UnitPrice = Clone(unitPrice),
                LineTotal = new Money
                {
                    Currency = unitPrice.Currency,
                    Amount = unitPrice.Amount * item.Quantity
                },
                Customizations = item.Customizations is null
                    ? null
                    : new Dictionary<string, string>(item.Customizations, StringComparer.OrdinalIgnoreCase)
            };
        }).ToList());
    }

    private static PaymentSummary BuildPayment(PaymentInstruction instruction, string currency, decimal totalAmount, DateTimeOffset now)
    {
        var reference = string.IsNullOrWhiteSpace(instruction.Reference)
            ? $"AUTH-{Guid.NewGuid():N}"
            : instruction.Reference!;

        var capturePending = !instruction.CaptureImmediately;

        return new PaymentSummary
        {
            Method = instruction.Method,
            Reference = reference,
            CapturePending = capturePending,
            AmountAuthorized = totalAmount,
            AmountCaptured = capturePending ? 0 : totalAmount,
            Currency = currency,
            AuthorizedAt = now,
            CapturedAt = capturePending ? null : now,
            Card = instruction.Card is null ? null : Clone(instruction.Card),
            Metadata = instruction.Metadata is null
                ? null
                : new Dictionary<string, string>(instruction.Metadata, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static CustomerProfile Clone(CustomerProfile source)
    {
        return new CustomerProfile
        {
            Id = source.Id,
            Email = source.Email,
            FirstName = source.FirstName,
            LastName = source.LastName,
            LoyaltyTier = source.LoyaltyTier,
            Attributes = source.Attributes is null
                ? null
                : new Dictionary<string, string>(source.Attributes, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static Address Clone(Address source)
    {
        return new Address
        {
            Line1 = source.Line1,
            Line2 = source.Line2,
            City = source.City,
            Region = source.Region,
            PostalCode = source.PostalCode,
            Country = source.Country
        };
    }

    private static Money Clone(Money source)
    {
        return new Money
        {
            Currency = source.Currency,
            Amount = source.Amount
        };
    }

    private static CardPaymentDetails Clone(CardPaymentDetails source)
    {
        return new CardPaymentDetails
        {
            Last4 = source.Last4,
            Network = source.Network,
            ExpMonth = source.ExpMonth,
            ExpYear = source.ExpYear,
            Tokenized = source.Tokenized,
            BillingAddress = source.BillingAddress is null ? null : Clone(source.BillingAddress)
        };
    }

    private static void EnsureMetadata(Order order)
    {
        if (!order.Metadata.ContainsKey("order.total"))
        {
            order.Metadata["order.total"] = order.Total.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }

        if (!order.Metadata.ContainsKey("order.currency"))
        {
            order.Metadata["order.currency"] = order.Total.Currency;
        }
    }
}
