using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ordo.BuildingBlocks.Messaging.Abstractions;
using Ordo.Contracts;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Ordo.BuildingBlocks.Messaging.RabbitMq;

/// <summary>Hosts one consumer per subscription with manual acknowledgements.</summary>
public sealed class RabbitMqConsumerHost(
    RabbitMqConnection connection,
    RabbitMqOptions options,
    IEnumerable<ConsumerSubscription> subscriptions,
    IServiceProvider services,
    ILogger<RabbitMqConsumerHost> logger) : BackgroundService
{
    private static readonly CreateChannelOptions ConsumeChannelOptions = new(false, false, null, null);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using IChannel channel = await connection.CreateChannelAsync(ConsumeChannelOptions, stoppingToken);

        // The exchange may not exist yet when a service starts before its first publisher.
        await RabbitMqTopology.DeclareExchangeAsync(channel, options.ExchangeName, stoppingToken);
        await RabbitMqTopology.DeclareConsumerQueuesAsync(channel, options.ExchangeName, subscriptions, stoppingToken);
        await channel.BasicQosAsync(0, options.PrefetchCount, global: false, stoppingToken);

        var consumerTags = new List<string>();
        foreach (ConsumerSubscription subscription in subscriptions)
        {
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (_, args) => HandleAsync(channel, subscription, args, stoppingToken);
            consumerTags.Add(await channel.BasicConsumeAsync(
                queue: subscription.QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken));
        }

        var lifetime = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenRegistration registration = stoppingToken.Register(() => lifetime.TrySetResult());
        await lifetime.Task;

        foreach (string tag in consumerTags)
        {
            await channel.BasicCancelAsync(tag, noWait: false, CancellationToken.None);
        }
    }

    private async Task HandleAsync(
        IChannel channel,
        ConsumerSubscription subscription,
        BasicDeliverEventArgs args,
        CancellationToken cancellationToken)
    {
        ulong deliveryTag = args.DeliveryTag;
        try
        {
            IntegrationEventEnvelope envelope = RabbitMqEnvelopeHeaders.FromProperties(args.BasicProperties);
            if (!string.Equals(envelope.Type, subscription.WireType, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Expected wire type {subscription.WireType}, got {envelope.Type}.");
            }

            string json = Encoding.UTF8.GetString(args.Body.ToArray());
            if (JsonSerializer.Deserialize(json, subscription.EventType, MessagingJson.Options) is not IIntegrationEvent integrationEvent)
            {
                throw new InvalidDataException("Message body is not an integration event.");
            }

            await subscription.Dispatch(services, integrationEvent, cancellationToken);
            await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken);
        }
        catch (Exception ex) when (IsPermanentFailure(ex))
        {
            logger.LogError(ex, "Permanent failure for {Queue}; message is dropped. TODO(phase-2): dead-letter to {Queue}.dlq", subscription.QueueName, subscription.QueueName);
            await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Transient failure for {Queue}; message will be requeued", subscription.QueueName);
            await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: true, cancellationToken: cancellationToken);
        }
    }

    private static bool IsPermanentFailure(Exception exception) =>
        exception is InvalidDataException
            or JsonException
            or FormatException
            or ArgumentException
            or NotSupportedException;
}
