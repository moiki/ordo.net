namespace Ordo.Orders.Domain;

public sealed record OrderStockRejected(
    Guid OrderId,
    string Reason,
    Guid EventId,
    DateTimeOffset OccurredAtUtc) : DomainEvent(EventId, OccurredAtUtc);
