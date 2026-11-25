namespace Ordo.Inventory.Application;

public sealed record StockDto(Guid ProductId, int AvailableQuantity);
