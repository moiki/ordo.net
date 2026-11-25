namespace Ordo.Inventory.Application;

public sealed record SetStockCommand(Guid ProductId, int Quantity);
