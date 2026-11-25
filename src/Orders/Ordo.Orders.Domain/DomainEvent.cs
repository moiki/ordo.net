namespace Ordo.Orders.Domain;

public abstract record DomainEvent(Guid EventId, DateTimeOffset OccurredAtUtc) : IDomainEvent;
