namespace Ordo.Orders.Domain;

public sealed record OrderStockReserved(
    Guid OrderId,
    Guid EventId,
    DateTimeOffset OccurredAtUtc) : DomainEvent(EventId, OccurredAtUtc);
