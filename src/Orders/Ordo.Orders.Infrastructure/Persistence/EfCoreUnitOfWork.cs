using Ordo.Orders.Application;

namespace Ordo.Orders.Infrastructure.Persistence;

internal sealed class EfCoreUnitOfWork(OrdersDbContext context) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
