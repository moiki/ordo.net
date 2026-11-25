using System.Text;
using Ordo.BuildingBlocks.Messaging.Abstractions;
using RabbitMQ.Client;

namespace Ordo.BuildingBlocks.Messaging.RabbitMq;

public sealed class RabbitMqBrokerPublisher(
    RabbitMqConnection connection,
    RabbitMqOptions options) : IBrokerPublisher, IAsyncDisposable
{
    private static readonly CreateChannelOptions PublishChannelOptions = new(true, true, null, null);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IChannel? _channel;

    public async Task PublishAsync(OutboundMessage message, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _channel ??= await connection.CreateChannelAsync(PublishChannelOptions, cancellationToken);
            await RabbitMqTopology.DeclareExchangeAsync(_channel, options.ExchangeName, cancellationToken);

            // With publisher confirmation tracking enabled, BasicPublishAsync throws on nack or basic.return.
            await _channel.BasicPublishAsync(
                exchange: options.ExchangeName,
                routingKey: message.Envelope.Type,
                mandatory: true,
                basicProperties: RabbitMqEnvelopeHeaders.ToProperties(message.Envelope),
                body: Encoding.UTF8.GetBytes(message.Body),
                cancellationToken: cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        _gate.Dispose();
    }
}
