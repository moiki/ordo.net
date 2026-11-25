using Microsoft.EntityFrameworkCore;
using Ordo.Inventory.Application;

namespace Ordo.Inventory.Infrastructure.Persistence;

internal sealed class EfCoreUnitOfWork(InventoryDbContext context) : IUnitOfWork
{
    public async Task<SaveOutcome> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return SaveOutcome.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            // The caller retries with a fresh read; stale tracked entities must not leak into it.
            context.ChangeTracker.Clear();
            return SaveOutcome.ConcurrencyConflict;
        }
    }
}
