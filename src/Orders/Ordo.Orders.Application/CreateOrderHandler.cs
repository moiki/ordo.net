using Ordo.BuildingBlocks.Messaging.Abstractions;
using Ordo.Orders.Domain;

namespace Ordo.Orders.Application;

public sealed class CreateOrderHandler(
    IOrderRepository repository,
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher,
    TimeProvider timeProvider) : ICommandHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        Order order;
        try
        {
            OrderLine[] lines = command.Lines
                .Select(line => new OrderLine(line.ProductId, line.Quantity, line.UnitPriceMinor))
                .ToArray();

            order = Order.Place(
                Guid.CreateVersion7(),
                command.CustomerId,
                lines,
                command.Currency,
                timeProvider.GetUtcNow());
        }
        catch (ArgumentException exception)
        {
            return Result<Guid>.Failure(new Error("order.invalid", exception.Message));
        }

        await repository.AddAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        OrderPlaced placed = order.DomainEvents.OfType<OrderPlaced>().Single();

        // Phase 1 publishes after the commit. A crash between these two operations loses
        // the event; the outbox in Phase 2 closes that gap without changing this handler's port.
        await publisher.PublishAsync(OrderEventsMapper.ToOrderCreated(placed), cancellationToken);

        order.ClearDomainEvents();
        return Result<Guid>.Success(order.Id);
    }
}
