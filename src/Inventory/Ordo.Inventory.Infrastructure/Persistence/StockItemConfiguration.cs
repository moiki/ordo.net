using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordo.Inventory.Domain;

namespace Ordo.Inventory.Infrastructure.Persistence;

internal sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("stock_items");
        builder.HasKey(item => item.Id);
        // The product id is the natural key; one stock row per product.
        builder.Property(item => item.Id).ValueGeneratedNever().HasColumnName("product_id");
        builder.Property(item => item.AvailableQuantity).HasColumnName("available_quantity").IsRequired();

        // Npgsql maps a uint row version to PostgreSQL's xmin system column (no column is created).
        builder.Property(item => item.Version).IsRowVersion();
    }
}
