namespace Ordo.Orders.Domain;

public sealed record OrderPlaced(
    Guid OrderId,
    Guid CustomerId,
    IReadOnlyList<OrderLine> Lines,
    Money Total,
    Guid EventId,
    DateTimeOffset OccurredAtUtc) : DomainEvent(EventId, OccurredAtUtc);
