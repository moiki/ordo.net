namespace Ordo.Contracts.Inventory;

public sealed record StockReservedLineV1(
    Guid ProductId,
    int Quantity);
