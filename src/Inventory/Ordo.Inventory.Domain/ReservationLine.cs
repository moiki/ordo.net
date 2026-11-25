namespace Ordo.Inventory.Domain;

public sealed record ReservationLine
{
    public ReservationLine(Guid productId, int quantity)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product id is required.", nameof(productId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        ProductId = productId;
        Quantity = quantity;
    }

    public Guid ProductId { get; }

    public int Quantity { get; }
}
