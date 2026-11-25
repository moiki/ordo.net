namespace Ordo.Orders.Domain;

public sealed record OrderLine
{
    public OrderLine(Guid productId, int quantity, long unitPriceMinor)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product id is required.", nameof(productId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (unitPriceMinor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPriceMinor));
        }

        ProductId = productId;
        Quantity = quantity;
        UnitPriceMinor = unitPriceMinor;
    }

    public Guid ProductId { get; }

    public int Quantity { get; }

    public long UnitPriceMinor { get; }

    public long TotalMinor => checked(Quantity * UnitPriceMinor);
}
