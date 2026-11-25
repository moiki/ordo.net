using System.Text.Json.Serialization;

namespace Ordo.Contracts.Inventory;

public sealed record StockReservedV1 : IIntegrationEvent
{
    [JsonIgnore]
    public Guid EventId { get; init; }

    [JsonIgnore]
    public DateTimeOffset OccurredAtUtc { get; init; }

    [JsonIgnore]
    public string EventType => "inventory.stock-reserved.v1";

    [JsonIgnore]
    public string PartitionKey => OrderId.ToString();

    public required Guid OrderId { get; init; }

    public required Guid ReservationId { get; init; }

    public required IReadOnlyList<StockReservedLineV1> Lines { get; init; }

    public required long TotalMinor { get; init; }

    public required string Currency { get; init; }
}
