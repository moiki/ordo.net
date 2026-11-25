using System.Text.Json;
using System.Text.Json.Serialization;
using Ordo.BuildingBlocks.Messaging.Abstractions;
using Ordo.Contracts;
using Ordo.Contracts.Inventory;
using Ordo.Contracts.Orders;
using Shouldly;
using Xunit;

namespace Ordo.Messaging.Abstractions.Tests;

public sealed class EventMetadataTests
{
    private static readonly Guid OrderId = Guid.CreateVersion7();
    private static readonly Guid CorrelationId = Guid.CreateVersion7();

    [Fact]
    public void Body_should_not_contain_envelope_metadata()
    {
        var events = new IIntegrationEvent[]
        {
            NewOrderCreated(),
            NewStockReserved(),
            NewStockRejected()
        };

        foreach (IIntegrationEvent integrationEvent in events)
        {
            using var document = JsonDocument.Parse(MessagingJson.Serialize(integrationEvent));
            AssertNoProperty(document.RootElement, "eventId");
            AssertNoProperty(document.RootElement, "occurredAtUtc");
            AssertNoProperty(document.RootElement, "eventType");
            AssertNoProperty(document.RootElement, "partitionKey");
        }
    }

    [Fact]
    public void Wire_type_and_partition_key_should_follow_the_event_catalog()
    {
        NewOrderCreated().EventType.ShouldBe("orders.order-created.v1");
        NewStockReserved().EventType.ShouldBe("inventory.stock-reserved.v1");
        NewStockRejected().EventType.ShouldBe("inventory.stock-rejected.v1");

        NewOrderCreated().PartitionKey.ShouldBe(OrderId.ToString());
        NewStockReserved().PartitionKey.ShouldBe(OrderId.ToString());
        NewStockRejected().PartitionKey.ShouldBe(OrderId.ToString());
    }

    [Fact]
    public void Envelope_should_carry_the_event_metadata()
    {
        OrderCreatedV1 orderCreated = NewOrderCreated();
        var causationId = Guid.CreateVersion7();
        const string traceParent = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";

        var envelope = IntegrationEventEnvelope.From(
            orderCreated,
            CorrelationId,
            causationId,
            traceParent);

        envelope.MessageId.ShouldBe(orderCreated.EventId);
        envelope.Type.ShouldBe("orders.order-created.v1");
        envelope.OccurredOnUtc.ShouldBe(orderCreated.OccurredAtUtc);
        envelope.CorrelationId.ShouldBe(CorrelationId);
        envelope.CausationId.ShouldBe(causationId);
        envelope.TraceParent.ShouldBe(traceParent);
        envelope.PartitionKey.ShouldBe(OrderId.ToString());
    }

    [Fact]
    public void Body_should_use_camel_case_and_tolerate_unknown_properties()
    {
        var payload = new
        {
            orderId = OrderId,
            customerId = Guid.CreateVersion7(),
            lines = new[] { new { productId = Guid.CreateVersion7(), quantity = 1, unitPriceMinor = 100 } },
            totalMinor = 100L,
            currency = "EUR",
            futureField = "ignored"
        };

        OrderCreatedV1 deserialized = MessagingJson.Deserialize<OrderCreatedV1>(JsonSerializer.Serialize(payload))
            ?? throw new InvalidOperationException("Deserialization returned null.");

        deserialized.OrderId.ShouldBe(OrderId);
        deserialized.Currency.ShouldBe("EUR");
        deserialized.Lines.Single().Quantity.ShouldBe(1);

        using var output = JsonDocument.Parse(MessagingJson.Serialize(deserialized));
        output.RootElement.TryGetProperty("orderId", out _).ShouldBeTrue();
        output.RootElement.TryGetProperty("OrderId", out _).ShouldBeFalse();
    }

    [Fact]
    public void Enum_values_should_serialize_as_strings()
    {
        var sample = new SampleEvent
        {
            EventId = Guid.CreateVersion7(),
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Status = SampleStatus.Active
        };

        using var document = JsonDocument.Parse(MessagingJson.Serialize(sample));
        document.RootElement.GetProperty("status").GetString().ShouldBe("Active");
    }

    private static void AssertNoProperty(JsonElement element, string name) =>
        element.EnumerateObject()
            .Any(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            .ShouldBeFalse($"{name} must travel in headers, not in the JSON body.");

    private static OrderCreatedV1 NewOrderCreated() => new()
    {
        EventId = Guid.CreateVersion7(),
        OccurredAtUtc = DateTimeOffset.UtcNow,
        OrderId = OrderId,
        CustomerId = Guid.CreateVersion7(),
        Lines = [new OrderCreatedLineV1(Guid.CreateVersion7(), 1, 100)],
        TotalMinor = 100,
        Currency = "EUR"
    };

    private static StockReservedV1 NewStockReserved() => new()
    {
        EventId = Guid.CreateVersion7(),
        OccurredAtUtc = DateTimeOffset.UtcNow,
        OrderId = OrderId,
        ReservationId = Guid.CreateVersion7(),
        Lines = [new StockReservedLineV1(Guid.CreateVersion7(), 1)],
        TotalMinor = 100,
        Currency = "EUR"
    };

    private static StockRejectedV1 NewStockRejected() => new()
    {
        EventId = Guid.CreateVersion7(),
        OccurredAtUtc = DateTimeOffset.UtcNow,
        OrderId = OrderId,
        Reason = "insufficient stock",
        UnavailableProductIds = [Guid.CreateVersion7()]
    };

    public enum SampleStatus
    {
        Active
    }

    public sealed record SampleEvent : IIntegrationEvent
    {
        [JsonIgnore]
        public Guid EventId { get; init; }

        [JsonIgnore]
        public DateTimeOffset OccurredAtUtc { get; init; }

        [JsonIgnore]
        public string EventType => "test.sample.v1";

        [JsonIgnore]
        public string PartitionKey => "sample";

        public SampleStatus Status { get; init; }
    }
}
