using Ordo.Contracts.Inventory;
using Ordo.Inventory.Domain;

namespace Ordo.Inventory.Application;

internal static class InventoryEventsMapper
{
    public static StockReservedV1 ToStockReserved(ReservationPlaced placed) =>
        new()
        {
            EventId = placed.EventId,
            OccurredAtUtc = placed.OccurredAtUtc,
            OrderId = placed.OrderId,
            ReservationId = placed.ReservationId,
            Lines = placed.Lines
                .Select(line => new StockReservedLineV1(line.ProductId, line.Quantity))
                .ToArray(),
            TotalMinor = placed.TotalMinor,
            Currency = placed.Currency
        };

    public static StockRejectedV1 ToStockRejected(ReservationRejected rejected) =>
        new()
        {
            EventId = rejected.EventId,
            OccurredAtUtc = rejected.OccurredAtUtc,
            OrderId = rejected.OrderId,
            Reason = rejected.Reason,
            UnavailableProductIds = rejected.UnavailableProductIds.ToArray()
        };
}
