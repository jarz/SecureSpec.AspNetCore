namespace SecureSpec.AspNetCore.TestHost.Orders;

internal static class OrderSeedData
{
    public static IEnumerable<Order> CreateDefaults()
    {
        var now = DateTimeOffset.UtcNow;

        var first = OrderFactory.Create(new CreateOrderRequest
        {
            ClientReference = "B2B-0001",
            Customer = new CustomerProfile
            {
                Id = "cust-1001",
                Email = "casey@example.com",
                FirstName = "Casey",
                LastName = "Morgan",
                LoyaltyTier = "platinum",
                Attributes = new Dictionary<string, string>
                {
                    ["segment"] = "enterprise"
                }
            },
            ShippingAddress = new Address
            {
                Line1 = "400 Industrial Ave",
                City = "Austin",
                Region = "TX",
                PostalCode = "78701",
                Country = "US"
            },
            BillingAddress = new Address
            {
                Line1 = "400 Industrial Ave",
                City = "Austin",
                Region = "TX",
                PostalCode = "78701",
                Country = "US"
            },
            Items =
            {
                new CreateOrderItemRequest
                {
                    Sku = "WIDGET-PRO",
                    Name = "Widget Pro",
                    Quantity = 5,
                    UnitPrice = new Money { Currency = "USD", Amount = 199.95m },
                    Customizations = new Dictionary<string, string>
                    {
                        ["color"] = "graphite"
                    }
                },
                new CreateOrderItemRequest
                {
                    Sku = "SUPPORT-PLAN",
                    Name = "Priority Support",
                    Quantity = 1,
                    UnitPrice = new Money { Currency = "USD", Amount = 999.00m }
                }
            },
            Payment = new PaymentInstruction
            {
                Method = "invoice",
                CaptureImmediately = false,
                Metadata = new Dictionary<string, string>
                {
                    ["poNumber"] = "PO-48329"
                }
            },
            Notes = "Expedite fulfillment for trade show",
            Metadata = new Dictionary<string, string>
            {
                ["channel"] = "sales-team"
            }
        }, new Guid("fd9f6d62-80cb-4f2b-a8fb-9a5a56e87f02"), now.AddDays(-2));

        first.Status = OrderStatus.Delivered;
        first.UpdatedAt = first.CreatedAt.AddDays(1);
        first.AuditTrail.Add(new OrderAuditEntry(OrderStatus.Shipped, "Order shipped", first.CreatedAt.AddHours(30), null));
        first.AuditTrail.Add(new OrderAuditEntry(OrderStatus.Delivered, "Recipient confirmed delivery", first.UpdatedAt, null));

        var second = OrderFactory.Create(new CreateOrderRequest
        {
            ClientReference = "WEB-8891",
            Customer = new CustomerProfile
            {
                Id = "cust-2451",
                Email = "jules@example.org",
                FirstName = "Jules",
                LastName = "Rivera",
                LoyaltyTier = "gold"
            },
            ShippingAddress = new Address
            {
                Line1 = "19 Market Street",
                City = "Denver",
                Region = "CO",
                PostalCode = "80202",
                Country = "US"
            },
            Items =
            {
                new CreateOrderItemRequest
                {
                    Sku = "COURSE-INTRO",
                    Name = "Intro Training Course",
                    Quantity = 2,
                    UnitPrice = new Money { Currency = "USD", Amount = 149.00m }
                }
            },
            Payment = new PaymentInstruction
            {
                Method = "card",
                CaptureImmediately = true,
                Card = new CardPaymentDetails
                {
                    Last4 = "4242",
                    Network = "visa",
                    ExpMonth = 12,
                    ExpYear = DateTime.UtcNow.Year + 2,
                    Tokenized = true
                },
                Metadata = new Dictionary<string, string>
                {
                    ["channel"] = "web"
                }
            },
            Metadata = new Dictionary<string, string>
            {
                ["coupon"] = "SUMMER-24"
            }
        }, new Guid("f5b94eea-7f32-4c0a-8d16-9d589a4b8a04"), now.AddDays(-1));

        var third = OrderFactory.Create(new CreateOrderRequest
        {
            ClientReference = "MOBILE-204",
            Customer = new CustomerProfile
            {
                Id = "cust-3400",
                Email = "samir@example.net",
                FirstName = "Samir",
                LastName = "Patel"
            },
            ShippingAddress = new Address
            {
                Line1 = "77 Elm Street",
                City = "Portland",
                Region = "ME",
                PostalCode = "04101",
                Country = "US"
            },
            Items =
            {
                new CreateOrderItemRequest
                {
                    Sku = "APP-SUB",
                    Name = "App Subscription",
                    Quantity = 1,
                    UnitPrice = new Money { Currency = "USD", Amount = 19.99m }
                }
            },
            Payment = new PaymentInstruction
            {
                Method = "card",
                CaptureImmediately = false,
                Card = new CardPaymentDetails
                {
                    Last4 = "1881",
                    Network = "mastercard",
                    ExpMonth = 5,
                    ExpYear = DateTime.UtcNow.Year + 3,
                    Tokenized = true
                }
            }
        }, new Guid("4f5dd46c-3efd-447e-9d9a-1c3fa1a945bf"), now.AddHours(-6));

        second.Status = OrderStatus.Processing;
        second.AuditTrail.Add(new OrderAuditEntry(OrderStatus.Processing, "Packaging started", second.UpdatedAt.AddMinutes(45), null));
        second.Metadata["batch"] = "pack-22";

        third.Status = OrderStatus.Pending;

        return new[] { first, second, third };
    }
}
