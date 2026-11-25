using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordo.Orders.Domain;

namespace Ordo.Orders.Infrastructure.Persistence;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedNever();
        builder.Property(order => order.CustomerId).IsRequired();
        builder.Property(order => order.PlacedAtUtc).HasColumnName("placed_at_utc").IsRequired();
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(order => order.CancellationReason).HasMaxLength(512);

        // Money is a domain value object mapped as an EF complex type into two scalar columns.
        builder.ComplexProperty(order => order.Total, money =>
        {
            money.Property(value => value.AmountMinor).HasColumnName("total_minor").IsRequired();
            money.Property(value => value.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        });
        builder.Ignore(order => order.Lines);

        builder.OwnsMany<OrderLine>("_lines", lines =>
        {
            lines.ToTable("order_lines");
            // The same product may appear in different orders; only one line per order.
            lines.HasKey("OrderId", nameof(OrderLine.ProductId));
            lines.Property(line => line.ProductId).ValueGeneratedNever();
            lines.Property(line => line.Quantity).IsRequired();
            lines.Property(line => line.UnitPriceMinor).HasColumnName("unit_price_minor").IsRequired();
            lines.Ignore(line => line.TotalMinor);
        });
    }
}
