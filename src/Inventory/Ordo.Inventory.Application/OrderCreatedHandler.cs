using Ordo.BuildingBlocks.Messaging.Abstractions;
using Ordo.Contracts;
using Ordo.Contracts.Orders;
using Ordo.Inventory.Domain;

namespace Ordo.Inventory.Application;

/// <summary>
/// Reserves stock for an order and publishes the fact. Repeated deliveries are skipped because the
/// decision is persisted as a Reservation keyed by OrderId; the Inbox arrives in Phase 2.
/// A row-version conflict retries with a fresh read, so two orders competing for the last unit
/// yield exactly one reservation.
/// </summary>
public sealed class OrderCreatedHandler(
    IStockItemRepository stockItems,
    IReservationRepository reservations,
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher) : IIntegrationEventHandler<OrderCreatedV1>
{
    private const int MaxAttempts = 3;
    private const string InsufficientStockReason = "insufficient stock";

    public async Task HandleAsync(OrderCreatedV1 integrationEvent, CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            if (await reservations.GetByOrderIdAsync(integrationEvent.OrderId, cancellationToken) is not null)
            {
                return;
            }

            ReservationLine[] lines = integrationEvent.Lines
                .Select(line => new ReservationLine(line.ProductId, line.Quantity))
                .ToArray();

            IReadOnlyDictionary<Guid, StockItem> stock = await stockItems.GetByProductIdsAsync(
                lines.Select(line => line.ProductId).ToArray(),
                cancellationToken);

            Guid[] unavailableProductIds = lines
                .Where(line => !stock.TryGetValue(line.ProductId, out StockItem? item) || !item.CanReserve(line.Quantity))
                .Select(line => line.ProductId)
                .ToArray();

            var reservationId = Guid.CreateVersion7();
            Reservation reservation = unavailableProductIds.Length == 0
                ? Reservation.Reserve(
                    integrationEvent.OrderId,
                    reservationId,
                    lines,
                    integrationEvent.TotalMinor,
                    integrationEvent.Currency,
                    integrationEvent.OccurredAtUtc)
                : Reservation.Reject(
                    integrationEvent.OrderId,
                    reservationId,
                    InsufficientStockReason,
                    unavailableProductIds,
                    integrationEvent.TotalMinor,
                    integrationEvent.Currency,
                    integrationEvent.OccurredAtUtc);

            if (unavailableProductIds.Length == 0)
            {
                foreach (ReservationLine line in lines)
                {
                    stock[line.ProductId].Reserve(line.Quantity);
                }
            }

            await reservations.AddAsync(reservation, cancellationToken);

            SaveOutcome outcome = await unitOfWork.SaveChangesAsync(cancellationToken);
            if (outcome == SaveOutcome.ConcurrencyConflict)
            {
                if (attempt == MaxAttempts)
                {
                    // Transient: the consumer host nacks with requeue.
                    throw new InvalidOperationException(
                        $"Reservation for order {integrationEvent.OrderId} conflicted after {MaxAttempts} attempts.");
                }

                continue;
            }

            IIntegrationEvent published = reservation.DomainEvents.Single() switch
            {
                ReservationPlaced placed => InventoryEventsMapper.ToStockReserved(placed),
                ReservationRejected rejected => InventoryEventsMapper.ToStockRejected(rejected),
                var domainEvent => throw new InvalidOperationException(
                    $"Unexpected reservation domain event {domainEvent.GetType().Name}.")
            };

            await publisher.PublishAsync(published, cancellationToken);
            reservation.ClearDomainEvents();
            return;
        }
    }
}
