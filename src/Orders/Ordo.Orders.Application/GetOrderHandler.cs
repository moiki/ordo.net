using Ordo.Orders.Domain;

namespace Ordo.Orders.Application;

public sealed class GetOrderHandler(IOrderRepository repository)
    : IQueryHandler<GetOrderQuery, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> HandleAsync(
        GetOrderQuery query,
        CancellationToken cancellationToken)
    {
        Order? order = await repository.GetByIdAsync(query.OrderId, cancellationToken);
        if (order is null)
        {
            return Result<OrderDto>.Failure(new Error("order.not-found", $"Order {query.OrderId} was not found."));
        }

        return Result<OrderDto>.Success(new OrderDto(
            order.Id,
            order.CustomerId,
            order.Status,
            order.Total.AmountMinor,
            order.Total.Currency,
            order.CancellationReason));
    }
}
