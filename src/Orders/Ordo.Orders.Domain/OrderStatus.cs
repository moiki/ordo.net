namespace Ordo.Orders.Domain;

public enum OrderStatus
{
    Pending,
    AwaitingPayment,
    Confirmed,
    Cancelled
}
