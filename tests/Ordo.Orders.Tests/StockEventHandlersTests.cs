using Ordo.Contracts.Inventory;
using Ordo.Orders.Application;
using Ordo.Orders.Domain;
using Shouldly;
using Xunit;

namespace Ordo.Orders.Tests;

public sealed class StockEventHandlersTests
{
    private static readonly DateTimeOffset OccurredAtUtc = new(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task StockReservedHandler_should_move_to_awaiting_payment_and_save_once()
    {
        Order order = NewOrder();
        var repository = new TestDoubles.OrderRepository();
        repository.Seed(order);
        var calls = new List<string>();
        var handler = new StockReservedHandler(repository, new TestDoubles.UnitOfWork(calls));

        await handler.HandleAsync(Reserved(order.Id), CancellationToken.None);

        order.Status.ShouldBe(OrderStatus.AwaitingPayment);
        order.DomainEvents.ShouldBeEmpty();
        calls.ShouldBe(["save"]);
    }

    [Fact]
    public async Task StockReservedHandler_should_ignore_a_duplicate_delivery()
    {
        Order order = NewOrder();
        var repository = new TestDoubles.OrderRepository();
        repository.Seed(order);
        var calls = new List<string>();
        var handler = new StockReservedHandler(repository, new TestDoubles.UnitOfWork(calls));
        StockReservedV1 integrationEvent = Reserved(order.Id);

        await handler.HandleAsync(integrationEvent, CancellationToken.None);
        await handler.HandleAsync(integrationEvent, CancellationToken.None);

        order.Status.ShouldBe(OrderStatus.AwaitingPayment);
        calls.ShouldBe(["save"]);
    }

    [Fact]
    public async Task StockRejectedHandler_should_cancel_and_save_once()
    {
        Order order = NewOrder();
        var repository = new TestDoubles.OrderRepository();
        repository.Seed(order);
        var calls = new List<string>();
        var handler = new StockRejectedHandler(repository, new TestDoubles.UnitOfWork(calls));

        await handler.HandleAsync(Rejected(order.Id, "out of stock"), CancellationToken.None);

        order.Status.ShouldBe(OrderStatus.Cancelled);
        order.CancellationReason.ShouldBe("out of stock");
        order.DomainEvents.ShouldBeEmpty();
        calls.ShouldBe(["save"]);
    }

    [Fact]
    public async Task StockRejectedHandler_should_ignore_a_duplicate_delivery()
    {
        Order order = NewOrder();
        var repository = new TestDoubles.OrderRepository();
        repository.Seed(order);
        var calls = new List<string>();
        var handler = new StockRejectedHandler(repository, new TestDoubles.UnitOfWork(calls));
        StockRejectedV1 integrationEvent = Rejected(order.Id, "out of stock");

        await handler.HandleAsync(integrationEvent, CancellationToken.None);
        await handler.HandleAsync(integrationEvent, CancellationToken.None);

        order.Status.ShouldBe(OrderStatus.Cancelled);
        calls.ShouldBe(["save"]);
    }

    [Fact]
    public async Task StockReservedHandler_should_fail_when_the_order_does_not_exist()
    {
        var repository = new TestDoubles.OrderRepository();
        var handler = new StockReservedHandler(repository, new TestDoubles.UnitOfWork([]));

        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(Reserved(Guid.CreateVersion7()), CancellationToken.None));
    }

    private static Order NewOrder() => Order.Place(
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        [new OrderLine(Guid.CreateVersion7(), 1, 1000)],
        "USD",
        OccurredAtUtc);

    private static StockReservedV1 Reserved(Guid orderId) => new()
    {
        EventId = Guid.CreateVersion7(),
        OccurredAtUtc = OccurredAtUtc,
        OrderId = orderId,
        ReservationId = Guid.CreateVersion7(),
        Lines = [new StockReservedLineV1(Guid.CreateVersion7(), 1)],
        TotalMinor = 1000,
        Currency = "USD"
    };

    private static StockRejectedV1 Rejected(Guid orderId, string reason) => new()
    {
        EventId = Guid.CreateVersion7(),
        OccurredAtUtc = OccurredAtUtc,
        OrderId = orderId,
        Reason = reason,
        UnavailableProductIds = [Guid.CreateVersion7()]
    };
}
