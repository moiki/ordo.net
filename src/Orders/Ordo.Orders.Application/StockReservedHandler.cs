using Ordo.BuildingBlocks.Messaging.Abstractions;
using Ordo.Contracts.Inventory;
using Ordo.Orders.Domain;

namespace Ordo.Orders.Application;

public sealed class StockReservedHandler(
    IOrderRepository repository,
    IUnitOfWork unitOfWork) : IIntegrationEventHandler<StockReservedV1>
{
    public async Task HandleAsync(StockReservedV1 integrationEvent, CancellationToken cancellationToken)
    {
        Order order = await repository.GetByIdAsync(integrationEvent.OrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order {integrationEvent.OrderId} was not found.");

        if (!order.MarkStockReserved(integrationEvent.OccurredAtUtc))
        {
            return;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        order.ClearDomainEvents();
    }
}
