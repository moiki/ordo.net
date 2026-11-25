using Ordo.BuildingBlocks.Messaging.Abstractions;

namespace Ordo.BuildingBlocks.Messaging.RabbitMq;

public sealed class RabbitMqBrokerPublisher : IBrokerPublisher
{
    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : class
    {
        throw new NotImplementedException("TODO(phase-1): publish message to RabbitMQ");
    }
}
