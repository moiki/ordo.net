using Ordo.Contracts.Orders;
using Ordo.Orders.Domain;

namespace Ordo.Orders.Application;

internal static class OrderEventsMapper
{
    public static OrderCreatedV1 ToOrderCreated(OrderPlaced domainEvent) =>
        new()
        {
            EventId = domainEvent.EventId,
            OccurredAtUtc = domainEvent.OccurredAtUtc,
            OrderId = domainEvent.OrderId,
            CustomerId = domainEvent.CustomerId,
            Lines = domainEvent.Lines
                .Select(line => new OrderCreatedLineV1(line.ProductId, line.Quantity, line.UnitPriceMinor))
                .ToArray(),
            TotalMinor = domainEvent.Total.AmountMinor,
            Currency = domainEvent.Total.Currency
        };
}
