namespace Ordo.BuildingBlocks.Messaging.Abstractions;

public interface IBrokerPublisher
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : class;
}
