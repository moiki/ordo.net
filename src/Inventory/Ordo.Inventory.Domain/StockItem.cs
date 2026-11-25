namespace Ordo.Inventory.Domain;

public sealed class StockItem : AggregateRoot
{
    // EF Core materialization constructor. Version is PostgreSQL xmin, populated by the database.
    private StockItem(Guid id, int availableQuantity, uint version)
    {
        Id = id;
        AvailableQuantity = availableQuantity;
        Version = version;
    }

    public Guid Id { get; }

    public Guid ProductId => Id;

    public int AvailableQuantity { get; private set; }

    // Optimistic concurrency token mapped to the xmin system column in Infrastructure.
    public uint Version { get; private set; }

    public static StockItem Create(Guid productId, int quantity)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product id is required.", nameof(productId));
        }

        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        return new StockItem(productId, quantity, 0);
    }

    public void SetQuantity(int quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        AvailableQuantity = quantity;
    }

    public bool CanReserve(int quantity) => quantity > 0 && AvailableQuantity >= quantity;

    public void Reserve(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (!CanReserve(quantity))
        {
            throw new InvalidOperationException(
                $"Product {Id} has {AvailableQuantity} units available; {quantity} were requested.");
        }

        AvailableQuantity -= quantity;
    }
}
