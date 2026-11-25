using RabbitMQ.Client;

namespace Ordo.BuildingBlocks.Messaging.RabbitMq;

/// <summary>Idempotent declarations. A service declares only its own queues.</summary>
public static class RabbitMqTopology
{
    public static async Task DeclareExchangeAsync(
        IChannel channel,
        string exchangeName,
        CancellationToken cancellationToken) =>
        await channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

    public static async Task DeclareConsumerQueuesAsync(
        IChannel channel,
        string exchangeName,
        IEnumerable<ConsumerSubscription> subscriptions,
        CancellationToken cancellationToken)
    {
        foreach (ConsumerSubscription subscription in subscriptions)
        {
            var arguments = new Dictionary<string, object?> { ["x-queue-type"] = "quorum" };

            await channel.QueueDeclareAsync(
                queue: subscription.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: arguments,
                cancellationToken: cancellationToken);

            await channel.QueueBindAsync(
                queue: subscription.QueueName,
                exchange: exchangeName,
                routingKey: subscription.WireType,
                arguments: null,
                cancellationToken: cancellationToken);
        }
    }
}
