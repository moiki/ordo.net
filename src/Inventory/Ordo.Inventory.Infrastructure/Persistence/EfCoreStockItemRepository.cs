using Microsoft.EntityFrameworkCore;
using Ordo.Inventory.Application;
using Ordo.Inventory.Domain;

namespace Ordo.Inventory.Infrastructure.Persistence;

internal sealed class EfCoreStockItemRepository(InventoryDbContext context) : IStockItemRepository
{
    public Task<StockItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken) =>
        context.StockItems.SingleOrDefaultAsync(item => item.Id == productId, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, StockItem>> GetByProductIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken) =>
        await context.StockItems
            .Where(item => productIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

    public async Task AddAsync(StockItem stockItem, CancellationToken cancellationToken) =>
        await context.StockItems.AddAsync(stockItem, cancellationToken);
}
