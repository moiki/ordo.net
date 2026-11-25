namespace Ordo.Inventory.Domain;

public sealed record ReservationRejected(
    Guid OrderId,
    Guid ReservationId,
    string Reason,
    IReadOnlyList<Guid> UnavailableProductIds,
    Guid EventId,
    DateTimeOffset OccurredAtUtc) : DomainEvent(EventId, OccurredAtUtc);
