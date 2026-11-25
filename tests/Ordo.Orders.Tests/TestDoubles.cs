using Ordo.BuildingBlocks.Messaging.Abstractions;
using Ordo.Contracts;
using Ordo.Orders.Application;
using Ordo.Orders.Domain;

namespace Ordo.Orders.Tests;

internal static class TestDoubles
{
    public sealed class OrderRepository : IOrderRepository
    {
        private readonly Dictionary<Guid, Order> _orders = [];

        public IReadOnlyCollection<Order> All => _orders.Values.ToArray();

        public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken) =>
            Task.FromResult(_orders.GetValueOrDefault(orderId));

        public Task AddAsync(Order order, CancellationToken cancellationToken)
        {
            _orders.Add(order.Id, order);
            return Task.CompletedTask;
        }

        public void Seed(Order order) => _orders.Add(order.Id, order);
    }

    public sealed class UnitOfWork(List<string> calls) : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            calls.Add("save");
            return Task.CompletedTask;
        }
    }

    public sealed class EventPublisher(List<string> calls) : IIntegrationEventPublisher
    {
        public List<IIntegrationEvent> Published { get; } = [];

        public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            calls.Add("publish");
            Published.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }

    public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
