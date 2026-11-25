using Ordo.Orders.Domain;
using Shouldly;
using Xunit;

namespace Ordo.Orders.Tests;

public sealed class OrderTests
{
    private static readonly DateTimeOffset OccurredAtUtc = new(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Place_should_compute_total_and_raise_order_placed()
    {
        Order order = NewOrder();

        order.Status.ShouldBe(OrderStatus.Pending);
        order.Total.AmountMinor.ShouldBe(4500);
        order.Total.Currency.ShouldBe("USD");
        order.Lines.Count.ShouldBe(2);
        OrderPlaced placed = order.DomainEvents.OfType<OrderPlaced>().ShouldHaveSingleItem();
        placed.OrderId.ShouldBe(order.Id);
        placed.CustomerId.ShouldBe(order.CustomerId);
        placed.Total.ShouldBe(order.Total);
        placed.OccurredAtUtc.ShouldBe(OccurredAtUtc);
    }

    [Fact]
    public void MarkStockReserved_should_move_to_awaiting_payment_and_raise_event()
    {
        Order order = NewOrder();
        order.ClearDomainEvents();

        bool changed = order.MarkStockReserved(OccurredAtUtc);

        changed.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.AwaitingPayment);
        OrderStockReserved reserved = order.DomainEvents.OfType<OrderStockReserved>().ShouldHaveSingleItem();
        reserved.OrderId.ShouldBe(order.Id);
        reserved.OccurredAtUtc.ShouldBe(OccurredAtUtc);
    }

    [Fact]
    public void MarkStockReserved_should_be_idempotent()
    {
        Order order = NewOrder();
        order.MarkStockReserved(OccurredAtUtc);
        order.ClearDomainEvents();

        bool changed = order.MarkStockReserved(OccurredAtUtc);

        changed.ShouldBeFalse();
        order.Status.ShouldBe(OrderStatus.AwaitingPayment);
        order.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void RejectStock_should_cancel_and_raise_event()
    {
        Order order = NewOrder();
        order.ClearDomainEvents();

        bool changed = order.RejectStock("out of stock", OccurredAtUtc);

        changed.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Cancelled);
        order.CancellationReason.ShouldBe("out of stock");
        OrderStockRejected rejected = order.DomainEvents.OfType<OrderStockRejected>().ShouldHaveSingleItem();
        rejected.Reason.ShouldBe("out of stock");
        rejected.OrderId.ShouldBe(order.Id);
    }

    [Fact]
    public void RejectStock_should_be_idempotent_for_the_same_reason()
    {
        Order order = NewOrder();
        order.RejectStock("out of stock", OccurredAtUtc);
        order.ClearDomainEvents();

        bool changed = order.RejectStock("out of stock", OccurredAtUtc);

        changed.ShouldBeFalse();
        order.Status.ShouldBe(OrderStatus.Cancelled);
        order.DomainEvents.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(OrderStatus.AwaitingPayment)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Cancelled)]
    public void RejectStock_should_reject_invalid_states(OrderStatus status)
    {
        Order order = InState(status);

        Should.Throw<InvalidOrderStateException>(() => order.RejectStock("a different reason", OccurredAtUtc));
        order.Status.ShouldBe(status);
    }

    [Theory]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Cancelled)]
    public void MarkStockReserved_should_reject_invalid_states(OrderStatus status)
    {
        Order order = InState(status);

        Should.Throw<InvalidOrderStateException>(() => order.MarkStockReserved(OccurredAtUtc));
        order.Status.ShouldBe(status);
    }

    [Fact]
    public void ConfirmPayment_should_require_awaiting_payment()
    {
        Order order = NewOrder();

        Should.Throw<InvalidOrderStateException>(() => order.ConfirmPayment());
        order.MarkStockReserved(OccurredAtUtc);
        order.ConfirmPayment().ShouldBeTrue();
        order.ConfirmPayment().ShouldBeFalse();
        order.Status.ShouldBe(OrderStatus.Confirmed);
    }

    [Fact]
    public void CancelDueToPaymentFailure_should_require_awaiting_payment()
    {
        Order order = NewOrder();

        Should.Throw<InvalidOrderStateException>(() => order.CancelDueToPaymentFailure("declined"));
        order.MarkStockReserved(OccurredAtUtc);
        order.CancelDueToPaymentFailure("declined").ShouldBeTrue();
        order.CancelDueToPaymentFailure("declined").ShouldBeFalse();
        order.Status.ShouldBe(OrderStatus.Cancelled);
        order.CancellationReason.ShouldBe("declined");
    }

    [Fact]
    public void RejectStock_should_reject_a_blank_reason()
    {
        Order order = NewOrder();

        Should.Throw<ArgumentException>(() => order.RejectStock(" ", OccurredAtUtc));
        order.Status.ShouldBe(OrderStatus.Pending);
    }

    [Fact]
    public void Place_should_reject_an_empty_order()
    {
        Should.Throw<ArgumentException>(() => Order.Place(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            [],
            "USD",
            OccurredAtUtc));
    }

    [Fact]
    public void Place_should_reject_duplicate_product_lines()
    {
        var productId = Guid.CreateVersion7();

        Should.Throw<ArgumentException>(() => Order.Place(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            [
                new OrderLine(productId, 1, 100),
                new OrderLine(productId, 2, 100)
            ],
            "USD",
            OccurredAtUtc));
    }

    private static Order NewOrder() => Order.Place(
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        [
            new OrderLine(Guid.CreateVersion7(), 2, 1000),
            new OrderLine(Guid.CreateVersion7(), 1, 2500)
        ],
        "usd",
        OccurredAtUtc);

    private static Order InState(OrderStatus status)
    {
        Order order = NewOrder();
        switch (status)
        {
            case OrderStatus.AwaitingPayment:
                order.MarkStockReserved(OccurredAtUtc);
                break;
            case OrderStatus.Confirmed:
                order.MarkStockReserved(OccurredAtUtc);
                order.ConfirmPayment();
                break;
            case OrderStatus.Cancelled:
                order.RejectStock("out of stock", OccurredAtUtc);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }

        return order;
    }
}
