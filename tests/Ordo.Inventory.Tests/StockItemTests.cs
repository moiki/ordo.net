using Ordo.Inventory.Domain;
using Shouldly;
using Xunit;

namespace Ordo.Inventory.Tests;

public sealed class StockItemTests
{
    private static readonly Guid ProductId = Guid.CreateVersion7();

    [Fact]
    public void Create_should_set_product_id_and_available_quantity()
    {
        var item = StockItem.Create(ProductId, 7);

        item.Id.ShouldBe(ProductId);
        item.ProductId.ShouldBe(ProductId);
        item.AvailableQuantity.ShouldBe(7);
    }

    [Fact]
    public void Create_should_reject_an_empty_product_id()
    {
        Should.Throw<ArgumentException>(() => StockItem.Create(Guid.Empty, 1));
    }

    [Fact]
    public void Create_should_reject_a_negative_quantity()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => StockItem.Create(ProductId, -1));
    }

    [Fact]
    public void SetQuantity_should_replace_the_available_quantity()
    {
        var item = StockItem.Create(ProductId, 2);

        item.SetQuantity(9);

        item.AvailableQuantity.ShouldBe(9);
    }

    [Fact]
    public void SetQuantity_should_reject_a_negative_quantity()
    {
        var item = StockItem.Create(ProductId, 2);

        Should.Throw<ArgumentOutOfRangeException>(() => item.SetQuantity(-1));
        item.AvailableQuantity.ShouldBe(2);
    }

    [Theory]
    [InlineData(0, 1, false)]
    [InlineData(1, 1, true)]
    [InlineData(2, 1, true)]
    [InlineData(1, 2, false)]
    public void CanReserve_should_reflect_available_quantity(int available, int requested, bool expected)
    {
        var item = StockItem.Create(ProductId, available);

        item.CanReserve(requested).ShouldBe(expected);
    }

    [Fact]
    public void Reserve_should_decrement_available_quantity()
    {
        var item = StockItem.Create(ProductId, 5);

        item.Reserve(3);

        item.AvailableQuantity.ShouldBe(2);
    }

    [Fact]
    public void Reserve_should_reject_an_insufficient_quantity()
    {
        var item = StockItem.Create(ProductId, 1);

        Should.Throw<InvalidOperationException>(() => item.Reserve(2));
        item.AvailableQuantity.ShouldBe(1);
    }

    [Fact]
    public void Reserve_should_reject_a_non_positive_quantity()
    {
        var item = StockItem.Create(ProductId, 1);

        Should.Throw<ArgumentOutOfRangeException>(() => item.Reserve(0));
    }
}
