using Microsoft.EntityFrameworkCore;
using Ordo.Orders.Application;
using Ordo.Orders.Domain;

namespace Ordo.Orders.Infrastructure.Persistence;

internal sealed class EfCoreOrderRepository(OrdersDbContext context) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        context.Orders.SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);

    public async Task AddAsync(Order order, CancellationToken cancellationToken) =>
        await context.Orders.AddAsync(order, cancellationToken);
}
