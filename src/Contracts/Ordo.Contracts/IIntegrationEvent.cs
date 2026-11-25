namespace Ordo.Contracts;

public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAtUtc { get; }
}
