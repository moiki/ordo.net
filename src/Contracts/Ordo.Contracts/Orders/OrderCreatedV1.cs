using System.Text.Json.Serialization;

namespace Ordo.Contracts.Orders;

public sealed record OrderCreatedV1 : IIntegrationEvent
{
    [JsonIgnore]
    public Guid EventId { get; init; }

    [JsonIgnore]
    public DateTimeOffset OccurredAtUtc { get; init; }

    [JsonIgnore]
    public string EventType => "orders.order-created.v1";

    [JsonIgnore]
    public string PartitionKey => OrderId.ToString();

    public required Guid OrderId { get; init; }

    public required Guid CustomerId { get; init; }

    public required IReadOnlyList<OrderCreatedLineV1> Lines { get; init; }

    public required long TotalMinor { get; init; }

    public required string Currency { get; init; }
}
