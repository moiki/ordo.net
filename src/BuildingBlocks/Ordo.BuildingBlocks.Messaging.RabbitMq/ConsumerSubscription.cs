using Microsoft.Extensions.DependencyInjection;
using Ordo.BuildingBlocks.Messaging.Abstractions;
using Ordo.Contracts;

namespace Ordo.BuildingBlocks.Messaging.RabbitMq;

/// <summary>One consumer purpose: a queue, its wire type and a strongly typed handler dispatch.</summary>
public sealed record ConsumerSubscription(
    string QueueName,
    string WireType,
    Type EventType,
    Func<IServiceProvider, IIntegrationEvent, CancellationToken, Task> Dispatch)
{
    public static ConsumerSubscription For<TEvent>(string queueName, string wireType)
        where TEvent : IIntegrationEvent =>
        new(
            queueName,
            wireType,
            typeof(TEvent),
            static (services, integrationEvent, cancellationToken) =>
                services.GetRequiredService<IIntegrationEventHandler<TEvent>>()
                    .HandleAsync((TEvent)integrationEvent, cancellationToken));
}
