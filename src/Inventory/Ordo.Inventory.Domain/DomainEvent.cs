namespace Ordo.Inventory.Domain;

public abstract record DomainEvent(Guid EventId, DateTimeOffset OccurredAtUtc) : IDomainEvent;
