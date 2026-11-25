using Ordo.Inventory.Domain;
using Shouldly;
using Xunit;

namespace Ordo.Inventory.Tests;

public sealed class ReservationTests
{
    private static readonly DateTimeOffset OccurredAtUtc = new(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid OrderId = Guid.CreateVersion7();
    private static readonly Guid ReservationId = Guid.CreateVersion7();

    [Fact]
    public void Reserve_should_raise_placed_with_the_reserved_lines()
    {
        ReservationLine[] lines = new[] { new ReservationLine(Guid.CreateVersion7(), 2) };

        var reservation = Reservation.Reserve(OrderId, ReservationId, lines, 1998, "USD", OccurredAtUtc);

        reservation.OrderId.ShouldBe(OrderId);
        reservation.Id.ShouldBe(ReservationId);
        reservation.Status.ShouldBe(ReservationStatus.Reserved);
        reservation.RejectionReason.ShouldBeNull();
        reservation.TotalMinor.ShouldBe(1998);
        reservation.Currency.ShouldBe("USD");
        reservation.OccurredAtUtc.ShouldBe(OccurredAtUtc);
        reservation.Lines.ShouldHaveSingleItem().ShouldBe(lines[0]);

        ReservationPlaced placed = reservation.DomainEvents.OfType<ReservationPlaced>().ShouldHaveSingleItem();
        placed.OrderId.ShouldBe(OrderId);
        placed.ReservationId.ShouldBe(ReservationId);
        placed.TotalMinor.ShouldBe(1998);
        placed.Currency.ShouldBe("USD");
        placed.OccurredAtUtc.ShouldBe(OccurredAtUtc);
        placed.EventId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Reject_should_raise_rejected_with_the_unavailable_products()
    {
        Guid[] unavailable = new[] { Guid.CreateVersion7() };

        var reservation = Reservation.Reject(
            OrderId, ReservationId, "insufficient stock", unavailable, 1998, "USD", OccurredAtUtc);

        reservation.Status.ShouldBe(ReservationStatus.Rejected);
        reservation.RejectionReason.ShouldBe("insufficient stock");
        reservation.Lines.ShouldBeEmpty();

        ReservationRejected rejected = reservation.DomainEvents.OfType<ReservationRejected>().ShouldHaveSingleItem();
        rejected.OrderId.ShouldBe(OrderId);
        rejected.ReservationId.ShouldBe(ReservationId);
        rejected.Reason.ShouldBe("insufficient stock");
        rejected.UnavailableProductIds.ShouldBe(unavailable);
        rejected.OccurredAtUtc.ShouldBe(OccurredAtUtc);
    }

    [Fact]
    public void Reserve_should_reject_duplicate_product_lines()
    {
        var productId = Guid.CreateVersion7();

        Should.Throw<ArgumentException>(() => Reservation.Reserve(
            OrderId,
            ReservationId,
            [new ReservationLine(productId, 1), new ReservationLine(productId, 2)],
            100,
            "USD",
            OccurredAtUtc));
    }

    [Fact]
    public void Reserve_should_reject_an_empty_line_list()
    {
        Should.Throw<ArgumentException>(() => Reservation.Reserve(
            OrderId, ReservationId, [], 0, "USD", OccurredAtUtc));
    }

    [Fact]
    public void Reserve_should_reject_a_bad_currency()
    {
        Should.Throw<ArgumentException>(() => Reservation.Reserve(
            OrderId, ReservationId, [new ReservationLine(Guid.CreateVersion7(), 1)], 100, "US", OccurredAtUtc));
    }

    [Fact]
    public void Reject_should_reject_a_blank_reason()
    {
        Should.Throw<ArgumentException>(() => Reservation.Reject(
            OrderId, ReservationId, " ", [Guid.CreateVersion7()], 0, "USD", OccurredAtUtc));
    }

    [Fact]
    public void Reject_should_reject_an_empty_unavailable_list()
    {
        Should.Throw<ArgumentException>(() => Reservation.Reject(
            OrderId, ReservationId, "insufficient stock", [], 0, "USD", OccurredAtUtc));
    }

    [Fact]
    public void Reserve_should_reject_empty_identifiers()
    {
        ReservationLine[] lines = [new ReservationLine(Guid.CreateVersion7(), 1)];

        Should.Throw<ArgumentException>(() => Reservation.Reserve(Guid.Empty, ReservationId, lines, 1, "USD", OccurredAtUtc));
        Should.Throw<ArgumentException>(() => Reservation.Reserve(OrderId, Guid.Empty, lines, 1, "USD", OccurredAtUtc));
    }
}
