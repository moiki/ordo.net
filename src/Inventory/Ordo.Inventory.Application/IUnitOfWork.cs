namespace Ordo.Inventory.Application;

public interface IUnitOfWork
{
    Task<SaveOutcome> SaveChangesAsync(CancellationToken cancellationToken);
}
