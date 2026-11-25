using Ordo.Inventory.Domain;

namespace Ordo.Inventory.Application;

public interface IStockItemRepository
{
    Task<StockItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, StockItem>> GetByProductIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);

    Task AddAsync(StockItem stockItem, CancellationToken cancellationToken);
}
