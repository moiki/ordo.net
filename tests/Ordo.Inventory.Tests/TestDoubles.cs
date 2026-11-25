using Ordo.BuildingBlocks.Messaging.Abstractions;
using Ordo.Contracts;
using Ordo.Inventory.Application;
using Ordo.Inventory.Domain;

namespace Ordo.Inventory.Tests;

internal static class TestDoubles
{
    public sealed class StockItemRepository : IStockItemRepository
    {
        private readonly Dictionary<Guid, StockItem> _items = [];

        public IReadOnlyCollection<StockItem> All => _items.Values.ToArray();

        public Task<StockItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken) =>
            Task.FromResult(_items.GetValueOrDefault(productId));

        public Task<IReadOnlyDictionary<Guid, StockItem>> GetByProductIdsAsync(
            IReadOnlyCollection<Guid> productIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, StockItem>>(
                _items.Where(item => productIds.Contains(item.Key)).ToDictionary(item => item.Key, item => item.Value));

        public Task AddAsync(StockItem stockItem, CancellationToken cancellationToken)
        {
            _items.Add(stockItem.Id, stockItem);
            return Task.CompletedTask;
        }

        public void Seed(StockItem stockItem) => _items.Add(stockItem.Id, stockItem);

        public IReadOnlyDictionary<Guid, int> Snapshot() =>
            _items.ToDictionary(item => item.Key, item => item.Value.AvailableQuantity);

        public void Restore(IReadOnlyDictionary<Guid, int> snapshot)
        {
            foreach (Guid productId in _items.Keys.Except(snapshot.Keys).ToArray())
            {
                _items.Remove(productId);
            }

            foreach ((Guid productId, int quantity) in snapshot)
            {
                _items[productId].SetQuantity(quantity);
            }
        }
    }

    public sealed class ReservationRepository : IReservationRepository
    {
        private readonly Dictionary<Guid, Reservation> _reservations = [];
        private Reservation? _pending;

        public IReadOnlyCollection<Reservation> All => _reservations.Values.ToArray();

        public Task<Reservation?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken) =>
            Task.FromResult(_reservations.GetValueOrDefault(orderId));

        public Task AddAsync(Reservation reservation, CancellationToken cancellationToken)
        {
            _pending = reservation;
            return Task.CompletedTask;
        }

        public void Commit()
        {
            if (_pending is not null)
            {
                _reservations[_pending.OrderId] = _pending;
                _pending = null;
            }
        }

        public void Discard() => _pending = null;

        public void Seed(Reservation reservation) => _reservations[reservation.OrderId] = reservation;
    }

    public sealed class UnitOfWork(
        Func<int, SaveOutcome> outcome,
        Action? onSuccess = null,
        Action? onConflict = null) : IUnitOfWork
    {
        public int Calls { get; private set; }

        public Task<SaveOutcome> SaveChangesAsync(CancellationToken cancellationToken)
        {
            Calls++;
            SaveOutcome result = outcome(Calls);
            if (result == SaveOutcome.Success)
            {
                onSuccess?.Invoke();
            }
            else
            {
                onConflict?.Invoke();
            }

            return Task.FromResult(result);
        }
    }

    public sealed class EventPublisher : IIntegrationEventPublisher
    {
        public List<IIntegrationEvent> Published { get; } = [];

        public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            Published.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }
}
