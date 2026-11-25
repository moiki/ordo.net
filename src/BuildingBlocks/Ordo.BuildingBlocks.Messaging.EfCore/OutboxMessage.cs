namespace Ordo.BuildingBlocks.Messaging.EfCore;

public sealed class OutboxMessage
{
    public Guid Id { get; init; }

    public required string EventType { get; init; }

    public required string Payload { get; init; }

    public DateTimeOffset OccurredAtUtc { get; init; }

    public DateTimeOffset? ProcessedAtUtc { get; set; }
}
