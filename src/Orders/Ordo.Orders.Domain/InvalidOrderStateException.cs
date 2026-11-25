namespace Ordo.Orders.Domain;

public sealed class InvalidOrderStateException : InvalidOperationException
{
    public InvalidOrderStateException(Guid orderId, OrderStatus current, OrderStatus target)
        : base($"Order {orderId} cannot move from {current} to {target}.")
    {
        OrderId = orderId;
        Current = current;
        Target = target;
    }

    public Guid OrderId { get; }

    public OrderStatus Current { get; }

    public OrderStatus Target { get; }
}
