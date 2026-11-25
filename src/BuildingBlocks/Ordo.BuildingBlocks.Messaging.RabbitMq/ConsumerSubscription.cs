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
            static async (services, integrationEvent, cancellationToken) =>
            {
                // Handlers and DbContexts are scoped; the host resolves from the root provider.
                using IServiceScope scope = services.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<TEvent>>()
                    .HandleAsync((TEvent)integrationEvent, cancellationToken);
            });
}
