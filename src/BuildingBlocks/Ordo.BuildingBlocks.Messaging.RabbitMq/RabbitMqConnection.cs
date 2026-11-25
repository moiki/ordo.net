using RabbitMQ.Client;

namespace Ordo.BuildingBlocks.Messaging.RabbitMq;

/// <summary>Owns the single long-lived connection; channels are created per publisher/consumer.</summary>
public sealed class RabbitMqConnection : IAsyncDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public RabbitMqConnection(RabbitMqOptions options)
    {
        _factory = new ConnectionFactory
        {
            Uri = new Uri(options.ConnectionString),
            ClientProvidedName = "ordo"
        };
    }

    public async Task<IChannel> CreateChannelAsync(
        CreateChannelOptions channelOptions,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _connection ??= await _factory.CreateConnectionAsync(cancellationToken);
            return await _connection.CreateChannelAsync(channelOptions, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }

        _gate.Dispose();
    }
}
