using System.Text.RegularExpressions;

namespace SecureSpec.AspNetCore.TestHost.Orders;

internal static class OrderValidator
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex SkuRegex = new(@"^[A-Z0-9_-]{3,32}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static Dictionary<string, string[]> Validate(CreateOrderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        ValidateCustomer(request.Customer, errors);
        ValidateAddresses(request.ShippingAddress, request.BillingAddress, errors);
        ValidateItems(request.Items, errors);
        ValidatePayment(request.Payment, errors);

        return errors;
    }

    public static string? ValidateTransition(OrderStatus current, OrderStatus next)
    {
        if (current == next)
        {
            return null;
        }

        if (!AllowedTransitions.TryGetValue(current, out var allowed) || !allowed.Contains(next))
        {
            return $"Cannot transition order status from {current} to {next}.";
        }

        return null;
    }

    private static void ValidateCustomer(CustomerProfile customer, IDictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(customer.Email))
        {
            AddError(errors, "customer.email", "Email is required.");
            return;
        }

        if (!EmailRegex.IsMatch(customer.Email))
        {
            AddError(errors, "customer.email", "Email is not a valid address.");
        }
    }

    private static void ValidateAddresses(Address shipping, Address? billing, IDictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(shipping.Line1) || string.IsNullOrWhiteSpace(shipping.City) || string.IsNullOrWhiteSpace(shipping.PostalCode))
        {
            AddError(errors, "shippingAddress", "Shipping address must include line1, city, and postal code.");
        }

        if (billing is not null && string.Equals(billing.Country, shipping.Country, StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(billing.PostalCode))
        {
            AddError(errors, "billingAddress.postalCode", "Postal code is required when billing and shipping countries match.");
        }
    }

    private static void ValidateItems(IReadOnlyCollection<CreateOrderItemRequest> items, IDictionary<string, string[]> errors)
    {
        if (items.Count == 0)
        {
            AddError(errors, "items", "At least one line item is required.");
            return;
        }

        string? currency = null;
        var index = 0;
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Sku) || !SkuRegex.IsMatch(item.Sku))
            {
                AddError(errors, $"items[{index}].sku", "SKU must contain 3-32 alphanumeric characters.");
            }

            if (item.Quantity <= 0)
            {
                AddError(errors, $"items[{index}].quantity", "Quantity must be greater than zero.");
            }

            if (item.UnitPrice is null)
            {
                AddError(errors, $"items[{index}].unitPrice", "Unit price is required.");
            }
            else
            {
                if (item.UnitPrice.Amount <= 0)
                {
                    AddError(errors, $"items[{index}].unitPrice.amount", "Unit price must be greater than zero.");
                }

                if (string.IsNullOrWhiteSpace(item.UnitPrice.Currency))
                {
                    AddError(errors, $"items[{index}].unitPrice.currency", "Currency is required.");
                }
                else if (currency is null)
                {
                    currency = item.UnitPrice.Currency;
                }
                else if (!string.Equals(currency, item.UnitPrice.Currency, StringComparison.OrdinalIgnoreCase))
                {
                    AddError(errors, "items.currency", "All items must share the same currency.");
                }
            }

            index++;
        }
    }

    private static void ValidatePayment(PaymentInstruction payment, IDictionary<string, string[]> errors)
    {
        if (payment is null)
        {
            AddError(errors, "payment", "Payment details are required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(payment.Method))
        {
            AddError(errors, "payment.method", "Payment method is required.");
        }

        if (string.Equals(payment.Method, "card", StringComparison.OrdinalIgnoreCase) && payment.Card is not null)
        {
            if (string.IsNullOrWhiteSpace(payment.Card.Last4) || payment.Card.Last4.Length != 4)
            {
                AddError(errors, "payment.card.last4", "Card last4 must contain exactly 4 digits.");
            }

            if (payment.Card.ExpMonth is < 1 or > 12)
            {
                AddError(errors, "payment.card.expMonth", "Expiration month must be between 1 and 12.");
            }

            if (payment.Card.ExpYear < DateTime.UtcNow.Year)
            {
                AddError(errors, "payment.card.expYear", "Expiration year must be in the future.");
            }
        }
    }

    private static void AddError(IDictionary<string, string[]> errors, string key, string message)
    {
        if (errors.TryGetValue(key, out var existing))
        {
            errors[key] = existing.Concat(new[] { message }).ToArray();
        }
        else
        {
            errors[key] = new[] { message };
        }
    }

    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.Pending] = new[] { OrderStatus.Processing, OrderStatus.Cancelled },
        [OrderStatus.Processing] = new[] { OrderStatus.Packed, OrderStatus.Cancelled },
        [OrderStatus.Packed] = new[] { OrderStatus.Shipped, OrderStatus.Cancelled },
        [OrderStatus.Shipped] = new[] { OrderStatus.Delivered, OrderStatus.Refunded },
        [OrderStatus.Delivered] = Array.Empty<OrderStatus>(),
        [OrderStatus.Cancelled] = Array.Empty<OrderStatus>(),
        [OrderStatus.Refunded] = Array.Empty<OrderStatus>()
    };
}
