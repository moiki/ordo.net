using Ordo.Inventory.Application;
using Ordo.Inventory.Domain;
using Shouldly;
using Xunit;

namespace Ordo.Inventory.Tests;

public sealed class SetStockHandlerTests
{
    private static readonly Guid ProductId = Guid.CreateVersion7();

    [Fact]
    public async Task HandleAsync_should_create_a_missing_stock_item()
    {
        var stockItems = new TestDoubles.StockItemRepository();
        var unitOfWork = new TestDoubles.UnitOfWork(_ => SaveOutcome.Success);
        var handler = new SetStockHandler(stockItems, unitOfWork);

        Result<StockDto> result = await handler.HandleAsync(new SetStockCommand(ProductId, 7), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ProductId.ShouldBe(ProductId);
        result.Value.AvailableQuantity.ShouldBe(7);
        stockItems.All.ShouldHaveSingleItem().AvailableQuantity.ShouldBe(7);
        unitOfWork.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_should_replace_an_existing_quantity()
    {
        var stockItems = new TestDoubles.StockItemRepository();
        stockItems.Seed(StockItem.Create(ProductId, 2));
        var unitOfWork = new TestDoubles.UnitOfWork(_ => SaveOutcome.Success);
        var handler = new SetStockHandler(stockItems, unitOfWork);

        Result<StockDto> result = await handler.HandleAsync(new SetStockCommand(ProductId, 9), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.AvailableQuantity.ShouldBe(9);
        stockItems.All.ShouldHaveSingleItem().AvailableQuantity.ShouldBe(9);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, -1)]
    public async Task HandleAsync_should_reject_an_invalid_command(bool emptyProductId, int quantity)
    {
        var stockItems = new TestDoubles.StockItemRepository();
        var unitOfWork = new TestDoubles.UnitOfWork(_ => SaveOutcome.Success);
        var handler = new SetStockHandler(stockItems, unitOfWork);
        Guid productId = emptyProductId ? Guid.Empty : ProductId;

        Result<StockDto> result = await handler.HandleAsync(new SetStockCommand(productId, quantity), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("stock.invalid");
        stockItems.All.ShouldBeEmpty();
        unitOfWork.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_should_retry_once_after_a_row_version_conflict()
    {
        var stockItems = new TestDoubles.StockItemRepository();
        stockItems.Seed(StockItem.Create(ProductId, 2));
        IReadOnlyDictionary<Guid, int> snapshot = stockItems.Snapshot();
        var unitOfWork = new TestDoubles.UnitOfWork(
            attempt => attempt == 1 ? SaveOutcome.ConcurrencyConflict : SaveOutcome.Success,
            onConflict: () => stockItems.Restore(snapshot));
        var handler = new SetStockHandler(stockItems, unitOfWork);

        Result<StockDto> result = await handler.HandleAsync(new SetStockCommand(ProductId, 9), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.AvailableQuantity.ShouldBe(9);
        unitOfWork.Calls.ShouldBe(2);
    }

    [Fact]
    public async Task HandleAsync_should_report_a_conflict_after_three_attempts()
    {
        var stockItems = new TestDoubles.StockItemRepository();
        stockItems.Seed(StockItem.Create(ProductId, 2));
        var unitOfWork = new TestDoubles.UnitOfWork(_ => SaveOutcome.ConcurrencyConflict);
        var handler = new SetStockHandler(stockItems, unitOfWork);

        Result<StockDto> result = await handler.HandleAsync(new SetStockCommand(ProductId, 9), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("stock.conflict");
        unitOfWork.Calls.ShouldBe(3);
    }
}
