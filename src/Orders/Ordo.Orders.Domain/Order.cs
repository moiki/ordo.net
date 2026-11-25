namespace Ordo.Orders.Domain;

public sealed class Order : AggregateRoot
{
    // List<T> (not an array) because EF Core collection navigations must support Add/Remove.
    // The public surface stays an IReadOnlyList<T>, so the domain contract is unchanged.
    private readonly List<OrderLine> _lines;

    private Order(
        Guid id,
        Guid customerId,
        IEnumerable<OrderLine> lines,
        string currency,
        DateTimeOffset placedAtUtc)
    {
        _lines = lines.ToList();
        Id = id;
        CustomerId = customerId;
        PlacedAtUtc = placedAtUtc;
        Total = new Money(_lines.Sum(line => line.TotalMinor), currency);

        Raise(new OrderPlaced(
            id,
            customerId,
            _lines.AsReadOnly(),
            Total,
            Guid.CreateVersion7(),
            placedAtUtc));
    }

    // EF Core materialization constructor. Persisted columns map to these parameters;
    // domain events are not re-raised when loading and owned lines are set via their backing field.
    private Order(
        Guid id,
        Guid customerId,
        DateTimeOffset placedAtUtc,
        OrderStatus status,
        string? cancellationReason)
    {
        Id = id;
        CustomerId = customerId;
        PlacedAtUtc = placedAtUtc;
        Status = status;
        CancellationReason = cancellationReason;
        // EF Core sets Total (complex property) and _lines (owned collection) via backing fields.
        Total = null!;
        _lines = [];
    }

    public Guid Id { get; }

    public Guid CustomerId { get; }

    public DateTimeOffset PlacedAtUtc { get; }

    public OrderStatus Status { get; private set; } = OrderStatus.Pending;

    public string? CancellationReason { get; private set; }

    public Money Total { get; }

    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

    public static Order Place(
        Guid id,
        Guid customerId,
        IReadOnlyList<OrderLine> lines,
        string currency,
        DateTimeOffset placedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Order id is required.", nameof(id));
        }

        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("Customer id is required.", nameof(customerId));
        }

        if (lines.Count == 0)
        {
            throw new ArgumentException("An order needs at least one line.", nameof(lines));
        }

        // One line per product keeps the aggregate aligned with the order_lines primary key.
        if (lines.Select(line => line.ProductId).Distinct().Count() != lines.Count)
        {
            throw new ArgumentException("An order cannot contain duplicate product lines.", nameof(lines));
        }

        return new Order(id, customerId, lines, currency, placedAtUtc);
    }

    public bool MarkStockReserved(DateTimeOffset occurredAtUtc)
    {
        if (Status == OrderStatus.AwaitingPayment)
        {
            return false;
        }

        if (Status != OrderStatus.Pending)
        {
            throw new InvalidOrderStateException(Id, Status, OrderStatus.AwaitingPayment);
        }

        Status = OrderStatus.AwaitingPayment;
        Raise(new OrderStockReserved(Id, Guid.CreateVersion7(), occurredAtUtc));
        return true;
    }

    public bool RejectStock(string reason, DateTimeOffset occurredAtUtc)
    {
        ValidateReason(reason);

        if (Status == OrderStatus.Cancelled && string.Equals(CancellationReason, reason, StringComparison.Ordinal))
        {
            return false;
        }

        if (Status != OrderStatus.Pending)
        {
            throw new InvalidOrderStateException(Id, Status, OrderStatus.Cancelled);
        }

        CancellationReason = reason;
        Status = OrderStatus.Cancelled;
        Raise(new OrderStockRejected(Id, reason, Guid.CreateVersion7(), occurredAtUtc));
        return true;
    }

    public bool ConfirmPayment()
    {
        if (Status == OrderStatus.Confirmed)
        {
            return false;
        }

        if (Status != OrderStatus.AwaitingPayment)
        {
            throw new InvalidOrderStateException(Id, Status, OrderStatus.Confirmed);
        }

        Status = OrderStatus.Confirmed;
        return true;
    }

    public bool CancelDueToPaymentFailure(string reason)
    {
        ValidateReason(reason);

        if (Status == OrderStatus.Cancelled && string.Equals(CancellationReason, reason, StringComparison.Ordinal))
        {
            return false;
        }

        if (Status != OrderStatus.AwaitingPayment)
        {
            throw new InvalidOrderStateException(Id, Status, OrderStatus.Cancelled);
        }

        CancellationReason = reason;
        Status = OrderStatus.Cancelled;
        return true;
    }

    private static void ValidateReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A cancellation reason is required.", nameof(reason));
        }
    }
}
