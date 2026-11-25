namespace Ordo.BuildingBlocks.Messaging.Abstractions;

public interface IBrokerPublisher
{
    Task PublishAsync(OutboundMessage message, CancellationToken cancellationToken);
}
