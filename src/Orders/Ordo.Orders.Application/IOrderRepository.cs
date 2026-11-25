using Ordo.Orders.Domain;

namespace Ordo.Orders.Application;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task AddAsync(Order order, CancellationToken cancellationToken);
}
