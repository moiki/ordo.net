namespace Ordo.Orders.Application;

public sealed record CreateOrderCommand(
    Guid CustomerId,
    string Currency,
    IReadOnlyList<CreateOrderLine> Lines);
