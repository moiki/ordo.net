using Ordo.Contracts;

namespace Ordo.BuildingBlocks.Messaging.Abstractions;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
