namespace Ordo.Inventory.Domain;

public sealed record ReservationPlaced(
    Guid OrderId,
    Guid ReservationId,
    IReadOnlyList<ReservationLine> Lines,
    long TotalMinor,
    string Currency,
    Guid EventId,
    DateTimeOffset OccurredAtUtc) : DomainEvent(EventId, OccurredAtUtc);
