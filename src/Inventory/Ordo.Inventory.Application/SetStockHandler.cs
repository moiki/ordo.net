using Ordo.Inventory.Domain;

namespace Ordo.Inventory.Application;

/// <summary>
/// Dev-only admin use case used to seed stock in Phase 1. A row-version conflict retries with a
/// fresh read; after MaxAttempts it becomes a 409 at the HTTP boundary.
/// </summary>
public sealed class SetStockHandler(
    IStockItemRepository stockItems,
    IUnitOfWork unitOfWork) : ICommandHandler<SetStockCommand, Result<StockDto>>
{
    private const int MaxAttempts = 3;

    public async Task<Result<StockDto>> HandleAsync(
        SetStockCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ProductId == Guid.Empty)
        {
            return Result<StockDto>.Failure(new Error("stock.invalid", "Product id is required."));
        }

        if (command.Quantity < 0)
        {
            return Result<StockDto>.Failure(new Error("stock.invalid", "Quantity cannot be negative."));
        }

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            StockItem? item = await stockItems.GetByProductIdAsync(command.ProductId, cancellationToken);
            if (item is null)
            {
                item = StockItem.Create(command.ProductId, command.Quantity);
                await stockItems.AddAsync(item, cancellationToken);
            }
            else
            {
                item.SetQuantity(command.Quantity);
            }

            SaveOutcome outcome = await unitOfWork.SaveChangesAsync(cancellationToken);
            if (outcome == SaveOutcome.ConcurrencyConflict)
            {
                if (attempt == MaxAttempts)
                {
                    return Result<StockDto>.Failure(new Error(
                        "stock.conflict",
                        $"Stock for product {command.ProductId} was updated concurrently."));
                }

                continue;
            }

            return Result<StockDto>.Success(new StockDto(item.ProductId, item.AvailableQuantity));
        }

        throw new InvalidOperationException("Unreachable: the retry loop always returns.");
    }
}
