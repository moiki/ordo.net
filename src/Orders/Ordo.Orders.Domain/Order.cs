namespace Ordo.Orders.Domain;

public sealed class Order : AggregateRoot
{
    private readonly OrderLine[] _lines;

    private Order(
        Guid id,
        Guid customerId,
        IEnumerable<OrderLine> lines,
        string currency,
        DateTimeOffset placedAtUtc)
    {
        _lines = lines.ToArray();
        Id = id;
        CustomerId = customerId;
        PlacedAtUtc = placedAtUtc;
        Total = new Money(_lines.Sum(line => line.TotalMinor), currency);

        Raise(new OrderPlaced(
            id,
            customerId,
            Array.AsReadOnly(_lines),
            Total,
            Guid.CreateVersion7(),
            placedAtUtc));
    }

    public Guid Id { get; }

    public Guid CustomerId { get; }

    public DateTimeOffset PlacedAtUtc { get; }

    public OrderStatus Status { get; private set; } = OrderStatus.Pending;

    public string? CancellationReason { get; private set; }

    public Money Total { get; }

    public IReadOnlyList<OrderLine> Lines => Array.AsReadOnly(_lines);

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
