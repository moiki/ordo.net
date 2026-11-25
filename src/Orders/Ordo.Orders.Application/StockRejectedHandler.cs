using Ordo.BuildingBlocks.Messaging.Abstractions;
using Ordo.Contracts.Inventory;
using Ordo.Orders.Domain;

namespace Ordo.Orders.Application;

public sealed class StockRejectedHandler(
    IOrderRepository repository,
    IUnitOfWork unitOfWork) : IIntegrationEventHandler<StockRejectedV1>
{
    public async Task HandleAsync(StockRejectedV1 integrationEvent, CancellationToken cancellationToken)
    {
        Order order = await repository.GetByIdAsync(integrationEvent.OrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order {integrationEvent.OrderId} was not found.");

        if (!order.RejectStock(integrationEvent.Reason, integrationEvent.OccurredAtUtc))
        {
            return;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        order.ClearDomainEvents();
    }
}
