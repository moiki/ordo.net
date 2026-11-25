using Ordo.Orders.Domain;

namespace Ordo.Orders.Application;

public sealed record OrderDto(
    Guid Id,
    Guid CustomerId,
    OrderStatus Status,
    long TotalMinor,
    string Currency,
    string? CancellationReason);
