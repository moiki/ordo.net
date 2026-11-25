namespace Ordo.BuildingBlocks.Messaging.Abstractions;

/// <summary>A serialized event body plus the broker-neutral metadata that travels in headers.</summary>
public sealed record OutboundMessage(IntegrationEventEnvelope Envelope, string Body);
