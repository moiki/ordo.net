using Ordo.Contracts.Orders;
using Ordo.Orders.Application;
using Ordo.Orders.Domain;
using Shouldly;
using Xunit;

namespace Ordo.Orders.Tests;

public sealed class CreateOrderHandlerTests
{
    private static readonly DateTimeOffset OccurredAtUtc = new(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_should_commit_then_publish_order_created()
    {
        var calls = new List<string>();
        var repository = new TestDoubles.OrderRepository();
        var unitOfWork = new TestDoubles.UnitOfWork(calls);
        var publisher = new TestDoubles.EventPublisher(calls);
        var handler = new CreateOrderHandler(
            repository,
            unitOfWork,
            publisher,
            new TestDoubles.FixedTimeProvider(OccurredAtUtc));
        var command = new CreateOrderCommand(
            Guid.CreateVersion7(),
            "usd",
            [new CreateOrderLine(Guid.CreateVersion7(), 2, 999)]);

        Result<Guid> result = await handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        Guid orderId = result.Value;
        orderId.ShouldNotBe(Guid.Empty);
        Order stored = repository.All.ShouldHaveSingleItem();
        stored.Id.ShouldBe(orderId);
        stored.Status.ShouldBe(OrderStatus.Pending);
        stored.DomainEvents.ShouldBeEmpty();
        calls.ShouldBe(["save", "publish"]);

        OrderCreatedV1 published = publisher.Published.ShouldHaveSingleItem().ShouldBeOfType<OrderCreatedV1>();
        published.EventId.ShouldNotBe(Guid.Empty);
        published.OccurredAtUtc.ShouldBe(OccurredAtUtc);
        published.EventType.ShouldBe("orders.order-created.v1");
        published.PartitionKey.ShouldBe(orderId.ToString());
        published.OrderId.ShouldBe(orderId);
        published.CustomerId.ShouldBe(command.CustomerId);
        published.TotalMinor.ShouldBe(1998);
        published.Currency.ShouldBe("USD");
        OrderCreatedLineV1 line = published.Lines.ShouldHaveSingleItem();
        line.ProductId.ShouldBe(command.Lines[0].ProductId);
        line.Quantity.ShouldBe(2);
        line.UnitPriceMinor.ShouldBe(999);
    }

    [Fact]
    public async Task HandleAsync_should_return_failure_without_side_effects_for_an_invalid_command()
    {
        var calls = new List<string>();
        var repository = new TestDoubles.OrderRepository();
        var unitOfWork = new TestDoubles.UnitOfWork(calls);
        var publisher = new TestDoubles.EventPublisher(calls);
        var handler = new CreateOrderHandler(
            repository,
            unitOfWork,
            publisher,
            new TestDoubles.FixedTimeProvider(OccurredAtUtc));
        var command = new CreateOrderCommand(
            Guid.Empty,
            "USD",
            [new CreateOrderLine(Guid.CreateVersion7(), 1, 100)]);

        Result<Guid> result = await handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("order.invalid");
        repository.All.ShouldBeEmpty();
        calls.ShouldBeEmpty();
    }
}
