using Ordo.Contracts;

namespace Ordo.BuildingBlocks.Messaging.Abstractions;

/// <summary>Broker-neutral metadata carried in headers alongside the event body.</summary>
public sealed record IntegrationEventEnvelope(
    Guid MessageId,
    string Type,
    DateTimeOffset OccurredOnUtc,
    Guid CorrelationId,
    Guid? CausationId,
    string? TraceParent,
    string PartitionKey)
{
    public static IntegrationEventEnvelope From(
        IIntegrationEvent integrationEvent,
        Guid correlationId,
        Guid? causationId = null,
        string? traceParent = null) =>
        new(
            integrationEvent.EventId,
            integrationEvent.EventType,
            integrationEvent.OccurredAtUtc,
            correlationId,
            causationId,
            traceParent,
            integrationEvent.PartitionKey);
}
