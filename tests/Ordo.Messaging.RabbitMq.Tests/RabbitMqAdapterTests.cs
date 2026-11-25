using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ordo.BuildingBlocks.Messaging.Abstractions;
using Ordo.BuildingBlocks.Messaging.RabbitMq;
using Ordo.Contracts;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shouldly;
using Testcontainers.RabbitMq;
using Xunit;

namespace Ordo.Messaging.RabbitMq.Tests;

public sealed class RabbitMqAdapterTests : IAsyncLifetime
{
    private const string WireType = "tests.test-event.v1";
    private readonly RabbitMqContainer _container = new RabbitMqBuilder("rabbitmq:4.4.1-management-alpine").Build();
    private RabbitMqConnection _connection = null!;
    private RabbitMqOptions _options = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _options = new RabbitMqOptions
        {
            ConnectionString = _container.GetConnectionString(),
            ExchangeName = $"ordo.tests.{Guid.NewGuid():N}"
        };
        _connection = new RabbitMqConnection(_options);
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task Publish_should_use_confirms_persistence_headers_and_the_wire_routing_key()
    {
        string queue = NewQueue("publisher");
        await using IChannel channel = await NewChannelAsync();
        await DeclareAsync(channel, queue);

        TestEvent published = NewEvent();
        var correlationId = Guid.CreateVersion7();
        var causationId = Guid.CreateVersion7();
        var envelope = IntegrationEventEnvelope.From(published, correlationId, causationId, "traceparent-value");
        await PublishAsync(envelope, MessagingJson.Serialize(published));

        BasicDeliverEventArgs received = await ReceiveOneAsync(channel, queue);
        string json = Encoding.UTF8.GetString(received.Body.ToArray());
        json.ShouldBe(MessagingJson.Serialize(published));
        received.BasicProperties.Persistent.ShouldBeTrue();
        received.BasicProperties.Type.ShouldBe(WireType);
        received.BasicProperties.MessageId.ShouldBe(published.EventId.ToString());
        received.BasicProperties.CorrelationId.ShouldBe(correlationId.ToString());
        Header(received.BasicProperties, "causation_id").ShouldBe(causationId.ToString());
        Header(received.BasicProperties, "traceparent").ShouldBe("traceparent-value");
        Header(received.BasicProperties, "partition_key").ShouldBe(published.PartitionKey);

        IntegrationEventEnvelope decoded = RabbitMqEnvelopeHeaders.FromProperties(received.BasicProperties);
        decoded.MessageId.ShouldBe(envelope.MessageId);
        decoded.Type.ShouldBe(WireType);
        decoded.OccurredOnUtc.ShouldBe(envelope.OccurredOnUtc);
        decoded.CorrelationId.ShouldBe(correlationId);
        decoded.CausationId.ShouldBe(causationId);
        decoded.TraceParent.ShouldBe(envelope.TraceParent);
        decoded.PartitionKey.ShouldBe(published.PartitionKey);

        await channel.BasicAckAsync(received.DeliveryTag, multiple: false, CancellationToken.None);
    }

    [Fact]
    public async Task Consumer_should_dispatch_by_wire_type_and_ack()
    {
        string queue = NewQueue("ack");
        await DeclareAsync(await NewChannelAsync(), queue);
        RecordingHandler.Reset();
        CapturingLogger<RabbitMqConsumerHost> logger = new();
        await using ServiceProvider services = BuildServices(
            ConsumerSubscription.For<TestEvent>(queue, WireType),
            logger,
            new RecordingHandler());
        using RabbitMqConsumerHost host = CreateHost(services, logger);
        await host.StartAsync(CancellationToken.None);

        TestEvent published = NewEvent();
        await PublishAsync(Envelope(published), MessagingJson.Serialize(published));
        TestEvent handled = await RecordingHandler.Completed.Task.WaitAsync(TimeSpan.FromSeconds(20));

        handled.EventId.ShouldBe(published.EventId);
        logger.Messages.ShouldNotContain(message => message.Contains("failure", StringComparison.OrdinalIgnoreCase));

        await host.StopAsync(CancellationToken.None);
        await using IChannel probe = await NewChannelAsync();
        BasicGetResult? leftover = await probe.BasicGetAsync(queue, autoAck: true, CancellationToken.None);
        leftover.ShouldBeNull();
    }

    [Fact]
    public async Task Permanent_failure_should_nack_without_requeue()
    {
        string queue = NewQueue("permanent");
        await DeclareAsync(await NewChannelAsync(), queue);
        RecordingHandler.Reset();
        CapturingLogger<RabbitMqConsumerHost> logger = new();
        await using ServiceProvider services = BuildServices(
            ConsumerSubscription.For<TestEvent>(queue, WireType),
            logger,
            new RecordingHandler());
        using RabbitMqConsumerHost host = CreateHost(services, logger);
        await host.StartAsync(CancellationToken.None);

        TestEvent published = NewEvent();
        await PublishAsync(Envelope(published), "not-json");

        await WaitForLogAsync(logger, "Permanent failure");
        RecordingHandler.WasCalled.ShouldBeFalse();

        await host.StopAsync(CancellationToken.None);
        await using IChannel probe = await NewChannelAsync();
        BasicGetResult? leftover = await probe.BasicGetAsync(queue, autoAck: true, CancellationToken.None);
        leftover.ShouldBeNull();
    }

    [Fact]
    public async Task Unknown_wire_type_should_nack_without_requeue()
    {
        string queue = NewQueue("unknown-type");
        await DeclareAsync(await NewChannelAsync(), queue);
        RecordingHandler.Reset();
        CapturingLogger<RabbitMqConsumerHost> logger = new();
        await using ServiceProvider services = BuildServices(
            ConsumerSubscription.For<TestEvent>(queue, WireType),
            logger,
            new RecordingHandler());
        using RabbitMqConsumerHost host = CreateHost(services, logger);
        await host.StartAsync(CancellationToken.None);

        TestEvent published = NewEvent();
        await PublishRawAsync(WireType, Envelope(published, "tests.unknown.v1"), MessagingJson.Serialize(published));

        await WaitForLogAsync(logger, "Permanent failure");
        RecordingHandler.WasCalled.ShouldBeFalse();

        await host.StopAsync(CancellationToken.None);
        await using IChannel probe = await NewChannelAsync();
        BasicGetResult? leftover = await probe.BasicGetAsync(queue, autoAck: true, CancellationToken.None);
        leftover.ShouldBeNull();
    }

    [Fact]
    public async Task Transient_failure_should_requeue_for_a_second_delivery()
    {
        string queue = NewQueue("transient");
        await DeclareAsync(await NewChannelAsync(), queue);
        TransientHandler.Reset();
        CapturingLogger<RabbitMqConsumerHost> logger = new();
        await using ServiceProvider services = BuildServices(
            ConsumerSubscription.For<TestEvent>(queue, WireType),
            logger,
            new TransientHandler());
        using RabbitMqConsumerHost host = CreateHost(services, logger);
        await host.StartAsync(CancellationToken.None);

        TestEvent published = NewEvent();
        await PublishAsync(Envelope(published), MessagingJson.Serialize(published));

        await TransientHandler.Succeeded.Task.WaitAsync(TimeSpan.FromSeconds(20));
        TransientHandler.Attempts.ShouldBeGreaterThanOrEqualTo(2);
        logger.Messages.ShouldContain(message => message.Contains("Transient failure", StringComparison.Ordinal));

        await host.StopAsync(CancellationToken.None);
    }

    private async Task<IChannel> NewChannelAsync() =>
        await _connection.CreateChannelAsync(new CreateChannelOptions(false, false, null, null), CancellationToken.None);

    private async Task DeclareAsync(IChannel channel, string queue)
    {
        await RabbitMqTopology.DeclareExchangeAsync(channel, _options.ExchangeName, CancellationToken.None);
        await RabbitMqTopology.DeclareConsumerQueuesAsync(
            channel,
            _options.ExchangeName,
            [ConsumerSubscription.For<TestEvent>(queue, WireType)],
            CancellationToken.None);
    }

    private async Task PublishAsync(IntegrationEventEnvelope envelope, string body)
    {
        await using var publisher = new RabbitMqBrokerPublisher(_connection, _options);
        await publisher.PublishAsync(new OutboundMessage(envelope, body), CancellationToken.None);
    }

    private async Task PublishRawAsync(string routingKey, IntegrationEventEnvelope envelope, string body)
    {
        await using IChannel channel = await NewChannelAsync();
        await RabbitMqTopology.DeclareExchangeAsync(channel, _options.ExchangeName, CancellationToken.None);
        await channel.BasicPublishAsync(
            exchange: _options.ExchangeName,
            routingKey: routingKey,
            mandatory: true,
            basicProperties: RabbitMqEnvelopeHeaders.ToProperties(envelope),
            body: Encoding.UTF8.GetBytes(body),
            cancellationToken: CancellationToken.None);
    }

    private static IntegrationEventEnvelope Envelope(TestEvent integrationEvent, string? wireType = null) =>
        new(
            integrationEvent.EventId,
            wireType ?? WireType,
            integrationEvent.OccurredAtUtc,
            Guid.CreateVersion7(),
            null,
            null,
            integrationEvent.PartitionKey);

    private static string Header(IReadOnlyBasicProperties properties, string name)
    {
        properties.Headers.ShouldNotBeNull();
        object? value = properties.Headers![name];
        return value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string text => text,
            _ => throw new InvalidDataException($"Header {name} is not text.")
        };
    }

    private static async Task<BasicDeliverEventArgs> ReceiveOneAsync(IChannel channel, string queue)
    {
        var received = new TaskCompletionSource<BasicDeliverEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, args) =>
        {
            received.TrySetResult(args);
            return Task.CompletedTask;
        };
        await channel.BasicConsumeAsync(queue, autoAck: false, consumer: consumer, cancellationToken: CancellationToken.None);
        return await received.Task.WaitAsync(TimeSpan.FromSeconds(20));
    }

    private static ServiceProvider BuildServices(
        ConsumerSubscription subscription,
        ILogger<RabbitMqConsumerHost> logger,
        IIntegrationEventHandler<TestEvent> handler) =>
        new ServiceCollection()
            .AddSingleton(subscription)
            .AddSingleton(logger)
            .AddSingleton(handler)
            .BuildServiceProvider();

    private RabbitMqConsumerHost CreateHost(IServiceProvider services, ILogger<RabbitMqConsumerHost> logger) =>
        new(_connection, _options, [services.GetRequiredService<ConsumerSubscription>()], services, logger);

    private static async Task WaitForLogAsync(CapturingLogger<RabbitMqConsumerHost> logger, string text)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while (!logger.Messages.Any(message => message.Contains(text, StringComparison.Ordinal)))
        {
            await Task.Delay(25, timeout.Token);
        }
    }

    private string NewQueue(string purpose) => $"tests.{purpose}.{Guid.NewGuid():N}";

    private static TestEvent NewEvent() => new()
    {
        EventId = Guid.CreateVersion7(),
        OccurredAtUtc = DateTimeOffset.UtcNow,
        PartitionKey = Guid.NewGuid().ToString()
    };

    public sealed record TestEvent : IIntegrationEvent
    {
        public Guid EventId { get; init; }
        public DateTimeOffset OccurredAtUtc { get; init; }
        public string EventType => WireType;
        public string PartitionKey { get; init; } = string.Empty;
        public string Value => "payload";
    }

    public sealed class RecordingHandler : IIntegrationEventHandler<TestEvent>
    {
        public static TaskCompletionSource<TestEvent> Completed { get; private set; } = NewSource();

        public static bool WasCalled { get; private set; }

        public static void Reset()
        {
            Completed = NewSource();
            WasCalled = false;
        }

        public Task HandleAsync(TestEvent integrationEvent, CancellationToken cancellationToken)
        {
            WasCalled = true;
            Completed.TrySetResult(integrationEvent);
            return Task.CompletedTask;
        }

        private static TaskCompletionSource<TestEvent> NewSource() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class TransientHandler : IIntegrationEventHandler<TestEvent>
    {
        private static int _attempts;
        public static TaskCompletionSource<TestEvent> Succeeded { get; private set; } = NewSource();

        public static int Attempts => Volatile.Read(ref _attempts);

        public static void Reset()
        {
            Interlocked.Exchange(ref _attempts, 0);
            Succeeded = NewSource();
        }

        public Task HandleAsync(TestEvent integrationEvent, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _attempts) < 2)
            {
                throw new InvalidOperationException("transient handler failure");
            }

            Succeeded.TrySetResult(integrationEvent);
            return Task.CompletedTask;
        }

        private static TaskCompletionSource<TestEvent> NewSource() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        private readonly ConcurrentQueue<string> _messages = new();

        public IReadOnlyCollection<string> Messages => _messages.ToArray();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            _messages.Enqueue(formatter(state, exception));
    }
}
