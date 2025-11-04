using System.Text.Json;
using System.Text.Json.Serialization;

namespace SecureSpec.AspNetCore.TestHost.Orders;

public enum OrderMutationOutcome
{
    NotFound,
    ValidationFailed,
    Unchanged,
    Updated
}

public sealed class OrderRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, Order> _orders = new();

    public bool Any()
    {
        lock (_sync)
        {
            return _orders.Count > 0;
        }
    }

    public Order Add(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        lock (_sync)
        {
            _orders[order.Id] = Clone(order);
            return Clone(order);
        }
    }

    public IReadOnlyList<Order> List(OrderStatus? status, int? limit)
    {
        lock (_sync)
        {
            IEnumerable<Order> query = _orders.Values
                .OrderByDescending(order => order.CreatedAt);

            if (status.HasValue)
            {
                query = query.Where(order => order.Status == status.Value);
            }

            if (limit is > 0)
            {
                query = query.Take(limit.Value);
            }

            return query.Select(Clone).ToList();
        }
    }

    public bool TryGet(Guid id, out Order order)
    {
        lock (_sync)
        {
            if (_orders.TryGetValue(id, out var stored))
            {
                order = Clone(stored);
                return true;
            }
        }

        order = null!;
        return false;
    }

    public OrderMutationOutcome TryUpdateStatus(Guid id, OrderStatus nextStatus, string? reason, bool capturePayment, out Order updated, out string? error)
    {
        lock (_sync)
        {
            if (!_orders.TryGetValue(id, out var existing))
            {
                updated = null!;
                error = null;
                return OrderMutationOutcome.NotFound;
            }

            error = OrderValidator.ValidateTransition(existing.Status, nextStatus);
            if (error is not null)
            {
                updated = null!;
                return OrderMutationOutcome.ValidationFailed;
            }

            if (existing.Status == nextStatus && !capturePayment)
            {
                updated = Clone(existing);
                return OrderMutationOutcome.Unchanged;
            }

            existing.Status = nextStatus;
            existing.UpdatedAt = DateTimeOffset.UtcNow;

            if (capturePayment && existing.Payment.CapturePending)
            {
                existing.Payment = existing.Payment with
                {
                    CapturePending = false,
                    AmountCaptured = existing.Payment.AmountAuthorized,
                    CapturedAt = existing.UpdatedAt
                };
            }

            existing.AuditTrail.Add(new OrderAuditEntry(nextStatus, reason ?? $"Status changed to {nextStatus}", existing.UpdatedAt, reason));

            updated = Clone(existing);
            return OrderMutationOutcome.Updated;
        }
    }

    public bool TryDelete(Guid id)
    {
        lock (_sync)
        {
            return _orders.Remove(id);
        }
    }

    public OrderMutationOutcome ApplyMetadataPatch(Guid id, IDictionary<string, string?> changes, out Order updated)
    {
        ArgumentNullException.ThrowIfNull(changes);

        lock (_sync)
        {
            if (!_orders.TryGetValue(id, out var existing))
            {
                updated = null!;
                return OrderMutationOutcome.NotFound;
            }

            var now = DateTimeOffset.UtcNow;
            var mutating = false;

            foreach (var (key, value) in changes)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                if (value is null)
                {
                    if (existing.Metadata.Remove(key))
                    {
                        mutating = true;
                        existing.AuditTrail.Add(new OrderAuditEntry(existing.Status, $"Metadata '{key}' removed", now, null));
                    }
                }
                else
                {
                    mutating = true;
                    existing.Metadata[key] = value;
                    existing.AuditTrail.Add(new OrderAuditEntry(existing.Status, $"Metadata '{key}' set", now, value));
                }
            }

            if (!mutating)
            {
                updated = Clone(existing);
                return OrderMutationOutcome.Unchanged;
            }

            existing.UpdatedAt = now;
            updated = Clone(existing);
            return OrderMutationOutcome.Updated;
        }
    }

    public void Seed(IEnumerable<Order> orders)
    {
        ArgumentNullException.ThrowIfNull(orders);

        lock (_sync)
        {
            foreach (var order in orders)
            {
                _orders[order.Id] = Clone(order);
            }
        }
    }

    private static Order Clone(Order source)
    {
        var payload = JsonSerializer.Serialize(source, SerializerOptions);
        return JsonSerializer.Deserialize<Order>(payload, SerializerOptions)!;
    }

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };
}
