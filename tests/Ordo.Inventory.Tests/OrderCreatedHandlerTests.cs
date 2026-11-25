using Ordo.Contracts.Inventory;
using Ordo.Contracts.Orders;
using Ordo.Inventory.Application;
using Ordo.Inventory.Domain;
using Shouldly;
using Xunit;

namespace Ordo.Inventory.Tests;

public sealed class OrderCreatedHandlerTests
{
    private static readonly DateTimeOffset OccurredAtUtc = new(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_should_reserve_stock_and_publish_stock_reserved()
    {
        var productId = Guid.CreateVersion7();
        var orderId = Guid.CreateVersion7();
        var stockItems = new TestDoubles.StockItemRepository();
        stockItems.Seed(StockItem.Create(productId, 5));
        var reservations = new TestDoubles.ReservationRepository();
        var unitOfWork = new TestDoubles.UnitOfWork(_ => SaveOutcome.Success, reservations.Commit);
        var publisher = new TestDoubles.EventPublisher();
        var handler = new OrderCreatedHandler(stockItems, reservations, unitOfWork, publisher);

        await handler.HandleAsync(NewEvent(orderId, (productId, 2)), CancellationToken.None);

        unitOfWork.Calls.ShouldBe(1);
        stockItems.All.ShouldHaveSingleItem().AvailableQuantity.ShouldBe(3);
        Reservation reservation = reservations.All.ShouldHaveSingleItem();
        reservation.Status.ShouldBe(ReservationStatus.Reserved);
        reservation.DomainEvents.ShouldBeEmpty();

        StockReservedV1 published = publisher.Published.ShouldHaveSingleItem().ShouldBeOfType<StockReservedV1>();
        published.EventType.ShouldBe("inventory.stock-reserved.v1");
        published.PartitionKey.ShouldBe(orderId.ToString());
        published.OrderId.ShouldBe(orderId);
        published.ReservationId.ShouldBe(reservation.Id);
        published.TotalMinor.ShouldBe(200);
        published.Currency.ShouldBe("USD");
        published.OccurredAtUtc.ShouldBe(OccurredAtUtc);
        StockReservedLineV1 line = published.Lines.ShouldHaveSingleItem();
        line.ProductId.ShouldBe(productId);
        line.Quantity.ShouldBe(2);
    }

    [Fact]
    public async Task HandleAsync_should_reject_when_stock_is_insufficient()
    {
        var productId = Guid.CreateVersion7();
        var orderId = Guid.CreateVersion7();
        var stockItems = new TestDoubles.StockItemRepository();
        stockItems.Seed(StockItem.Create(productId, 1));
        var reservations = new TestDoubles.ReservationRepository();
        var unitOfWork = new TestDoubles.UnitOfWork(_ => SaveOutcome.Success, reservations.Commit);
        var publisher = new TestDoubles.EventPublisher();
        var handler = new OrderCreatedHandler(stockItems, reservations, unitOfWork, publisher);

        await handler.HandleAsync(NewEvent(orderId, (productId, 2)), CancellationToken.None);

        stockItems.All.ShouldHaveSingleItem().AvailableQuantity.ShouldBe(1);
        Reservation reservation = reservations.All.ShouldHaveSingleItem();
        reservation.Status.ShouldBe(ReservationStatus.Rejected);
        reservation.DomainEvents.ShouldBeEmpty();

        StockRejectedV1 published = publisher.Published.ShouldHaveSingleItem().ShouldBeOfType<StockRejectedV1>();
        published.EventType.ShouldBe("inventory.stock-rejected.v1");
        published.PartitionKey.ShouldBe(orderId.ToString());
        published.Reason.ShouldBe("insufficient stock");
        published.UnavailableProductIds.ShouldHaveSingleItem().ShouldBe(productId);
    }

    [Fact]
    public async Task HandleAsync_should_reject_a_product_without_a_stock_item()
    {
        var productId = Guid.CreateVersion7();
        var orderId = Guid.CreateVersion7();
        var stockItems = new TestDoubles.StockItemRepository();
        var reservations = new TestDoubles.ReservationRepository();
        var unitOfWork = new TestDoubles.UnitOfWork(_ => SaveOutcome.Success, reservations.Commit);
        var publisher = new TestDoubles.EventPublisher();
        var handler = new OrderCreatedHandler(stockItems, reservations, unitOfWork, publisher);

        await handler.HandleAsync(NewEvent(orderId, (productId, 1)), CancellationToken.None);

        StockRejectedV1 published = publisher.Published.ShouldHaveSingleItem().ShouldBeOfType<StockRejectedV1>();
        published.UnavailableProductIds.ShouldHaveSingleItem().ShouldBe(productId);
        reservations.All.ShouldHaveSingleItem().Status.ShouldBe(ReservationStatus.Rejected);
    }

    [Fact]
    public async Task HandleAsync_should_skip_a_duplicate_delivery()
    {
        var productId = Guid.CreateVersion7();
        var orderId = Guid.CreateVersion7();
        var stockItems = new TestDoubles.StockItemRepository();
        stockItems.Seed(StockItem.Create(productId, 5));
        var reservations = new TestDoubles.ReservationRepository();
        reservations.Seed(Reservation.Reserve(
            orderId,
            Guid.CreateVersion7(),
            [new ReservationLine(productId, 2)],
            200,
            "USD",
            OccurredAtUtc));
        var unitOfWork = new TestDoubles.UnitOfWork(_ => SaveOutcome.Success, reservations.Commit);
        var publisher = new TestDoubles.EventPublisher();
        var handler = new OrderCreatedHandler(stockItems, reservations, unitOfWork, publisher);

        await handler.HandleAsync(NewEvent(orderId, (productId, 2)), CancellationToken.None);

        unitOfWork.Calls.ShouldBe(0);
        publisher.Published.ShouldBeEmpty();
        stockItems.All.ShouldHaveSingleItem().AvailableQuantity.ShouldBe(5);
        reservations.All.Count.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_should_retry_once_after_a_row_version_conflict()
    {
        var productId = Guid.CreateVersion7();
        var orderId = Guid.CreateVersion7();
        var stockItems = new TestDoubles.StockItemRepository();
        stockItems.Seed(StockItem.Create(productId, 5));
        IReadOnlyDictionary<Guid, int> snapshot = stockItems.Snapshot();
        var reservations = new TestDoubles.ReservationRepository();
        var unitOfWork = new TestDoubles.UnitOfWork(
            attempt => attempt == 1 ? SaveOutcome.ConcurrencyConflict : SaveOutcome.Success,
            reservations.Commit,
            () =>
            {
                reservations.Discard();
                stockItems.Restore(snapshot);
            });
        var publisher = new TestDoubles.EventPublisher();
        var handler = new OrderCreatedHandler(stockItems, reservations, unitOfWork, publisher);

        await handler.HandleAsync(NewEvent(orderId, (productId, 2)), CancellationToken.None);

        unitOfWork.Calls.ShouldBe(2);
        stockItems.All.ShouldHaveSingleItem().AvailableQuantity.ShouldBe(3);
        reservations.All.Count.ShouldBe(1);
        publisher.Published.ShouldHaveSingleItem().ShouldBeOfType<StockReservedV1>();
    }

    [Fact]
    public async Task HandleAsync_should_fail_as_transient_after_three_conflicts()
    {
        var productId = Guid.CreateVersion7();
        var orderId = Guid.CreateVersion7();
        var stockItems = new TestDoubles.StockItemRepository();
        stockItems.Seed(StockItem.Create(productId, 5));
        var reservations = new TestDoubles.ReservationRepository();
        var unitOfWork = new TestDoubles.UnitOfWork(_ => SaveOutcome.ConcurrencyConflict);
        var publisher = new TestDoubles.EventPublisher();
        var handler = new OrderCreatedHandler(stockItems, reservations, unitOfWork, publisher);

        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(NewEvent(orderId, (productId, 2)), CancellationToken.None));

        unitOfWork.Calls.ShouldBe(3);
        publisher.Published.ShouldBeEmpty();
    }

    private static OrderCreatedV1 NewEvent(Guid orderId, params (Guid ProductId, int Quantity)[] lines) =>
        new()
        {
            EventId = Guid.CreateVersion7(),
            OccurredAtUtc = OccurredAtUtc,
            OrderId = orderId,
            CustomerId = Guid.CreateVersion7(),
            Lines = lines
                .Select(line => new OrderCreatedLineV1(line.ProductId, line.Quantity, 100))
                .ToArray(),
            TotalMinor = lines.Sum(line => line.Quantity * 100),
            Currency = "USD"
        };
}
