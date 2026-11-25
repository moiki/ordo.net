namespace Ordo.Inventory.Domain;

public sealed class Reservation : AggregateRoot
{
    // List<T> (not an array) because EF Core collection navigations must support Add/Remove.
    private readonly List<ReservationLine> _lines;

    // EF Core materialization constructor; lines are set through the backing field.
    private Reservation(
        Guid orderId,
        Guid id,
        ReservationStatus status,
        string? rejectionReason,
        long totalMinor,
        string currency,
        DateTimeOffset occurredAtUtc)
    {
        OrderId = orderId;
        Id = id;
        Status = status;
        RejectionReason = rejectionReason;
        TotalMinor = totalMinor;
        Currency = currency;
        OccurredAtUtc = occurredAtUtc;
        _lines = [];
    }

    public Guid OrderId { get; }

    public Guid Id { get; }

    public ReservationStatus Status { get; }

    public string? RejectionReason { get; }

    public long TotalMinor { get; }

    public string Currency { get; }

    public DateTimeOffset OccurredAtUtc { get; }

    public IReadOnlyList<ReservationLine> Lines => _lines.AsReadOnly();

    public static Reservation Reserve(
        Guid orderId,
        Guid reservationId,
        IReadOnlyList<ReservationLine> lines,
        long totalMinor,
        string currency,
        DateTimeOffset occurredAtUtc)
    {
        ValidateOrderId(orderId);
        ValidateReservationId(reservationId);
        ValidateLines(lines);
        ValidateTotals(totalMinor, currency);

        var reservation = new Reservation(
            orderId,
            reservationId,
            ReservationStatus.Reserved,
            null,
            totalMinor,
            currency,
            occurredAtUtc);
        reservation._lines.AddRange(lines);
        reservation.Raise(new ReservationPlaced(
            orderId,
            reservationId,
            reservation.Lines,
            totalMinor,
            currency,
            Guid.CreateVersion7(),
            occurredAtUtc));
        return reservation;
    }

    public static Reservation Reject(
        Guid orderId,
        Guid reservationId,
        string reason,
        IReadOnlyList<Guid> unavailableProductIds,
        long totalMinor,
        string currency,
        DateTimeOffset occurredAtUtc)
    {
        ValidateOrderId(orderId);
        ValidateReservationId(reservationId);
        ValidateTotals(totalMinor, currency);

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A rejection reason is required.", nameof(reason));
        }

        if (unavailableProductIds.Count == 0)
        {
            throw new ArgumentException("At least one unavailable product is required.", nameof(unavailableProductIds));
        }

        var reservation = new Reservation(
            orderId,
            reservationId,
            ReservationStatus.Rejected,
            reason,
            totalMinor,
            currency,
            occurredAtUtc);
        reservation.Raise(new ReservationRejected(
            orderId,
            reservationId,
            reason,
            unavailableProductIds.ToArray(),
            Guid.CreateVersion7(),
            occurredAtUtc));
        return reservation;
    }

    private static void ValidateOrderId(Guid orderId)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order id is required.", nameof(orderId));
        }
    }

    private static void ValidateReservationId(Guid reservationId)
    {
        if (reservationId == Guid.Empty)
        {
            throw new ArgumentException("Reservation id is required.", nameof(reservationId));
        }
    }

    private static void ValidateLines(IReadOnlyList<ReservationLine> lines)
    {
        if (lines.Count == 0)
        {
            throw new ArgumentException("A reservation needs at least one line.", nameof(lines));
        }

        if (lines.Select(line => line.ProductId).Distinct().Count() != lines.Count)
        {
            throw new ArgumentException("A reservation cannot contain duplicate product lines.", nameof(lines));
        }
    }

    private static void ValidateTotals(long totalMinor, string currency)
    {
        if (totalMinor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalMinor));
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3 || !currency.All(char.IsLetter))
        {
            throw new ArgumentException("Currency must be a three-letter ISO 4217 code.", nameof(currency));
        }
    }
}
