using Ordo.BuildingBlocks.Messaging.Abstractions;
using Ordo.Contracts;

namespace Ordo.BuildingBlocks.Messaging.RabbitMq;

/// <summary>
/// Phase 1 publisher: sends an integration event straight to the broker after the caller commits.
/// Phase 2 replaces this implementation with the outbox without changing Application code.
/// </summary>
public sealed class DirectIntegrationEventPublisher(IBrokerPublisher brokerPublisher)
    : IIntegrationEventPublisher
{
    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        // The aggregate id is the saga correlation id in Phase 1.
        Guid correlationId = Guid.TryParse(integrationEvent.PartitionKey, out Guid parsed)
            ? parsed
            : integrationEvent.EventId;

        var envelope = IntegrationEventEnvelope.From(integrationEvent, correlationId);
        return brokerPublisher.PublishAsync(
            new OutboundMessage(envelope, MessagingJson.Serialize(integrationEvent)),
            cancellationToken);
    }
}
