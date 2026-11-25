namespace Ordo.BuildingBlocks.Messaging.EfCore;

public sealed class InboxMessage
{
    public Guid EventId { get; init; }

    public required string HandlerName { get; init; }

    public DateTimeOffset ProcessedAtUtc { get; init; }
}
