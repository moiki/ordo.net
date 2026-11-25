namespace Ordo.Orders.Application;

public sealed record CreateOrderLine(Guid ProductId, int Quantity, long UnitPriceMinor);
