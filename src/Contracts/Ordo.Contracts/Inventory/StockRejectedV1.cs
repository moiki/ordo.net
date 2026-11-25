using System.Text.Json.Serialization;

namespace Ordo.Contracts.Inventory;

public sealed record StockRejectedV1 : IIntegrationEvent
{
    [JsonIgnore]
    public Guid EventId { get; init; }

    [JsonIgnore]
    public DateTimeOffset OccurredAtUtc { get; init; }

    [JsonIgnore]
    public string EventType => "inventory.stock-rejected.v1";

    [JsonIgnore]
    public string PartitionKey => OrderId.ToString();

    public required Guid OrderId { get; init; }

    public required string Reason { get; init; }

    public required IReadOnlyList<Guid> UnavailableProductIds { get; init; }
}
