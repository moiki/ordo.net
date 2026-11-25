namespace Ordo.Contracts.Orders;

public sealed record OrderCreatedLineV1(
    Guid ProductId,
    int Quantity,
    long UnitPriceMinor);
