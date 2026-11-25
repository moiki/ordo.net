namespace Ordo.Contracts;

public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAtUtc { get; }

    /// <summary>Stable wire type, for example "orders.order-created.v1". Never a .NET type name.</summary>
    string EventType { get; }

    /// <summary>Aggregate id; used for ordering and, in Kafka, as the message key.</summary>
    string PartitionKey { get; }
}
