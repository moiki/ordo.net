using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordo.Inventory.Domain;

namespace Ordo.Inventory.Infrastructure.Persistence;

internal sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations");
        // One decision per order: this key is also the Phase 1 idempotency guard.
        builder.HasKey(reservation => reservation.OrderId);
        builder.Property(reservation => reservation.OrderId).ValueGeneratedNever().HasColumnName("order_id");
        builder.Property(reservation => reservation.Id).ValueGeneratedNever().HasColumnName("id");
        builder.Property(reservation => reservation.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasColumnName("status")
            .IsRequired();
        builder.Property(reservation => reservation.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(512);
        builder.Property(reservation => reservation.TotalMinor).HasColumnName("total_minor").IsRequired();
        builder.Property(reservation => reservation.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(reservation => reservation.OccurredAtUtc).HasColumnName("occurred_at_utc").IsRequired();
        builder.Ignore(reservation => reservation.Lines);

        builder.OwnsMany<ReservationLine>("_lines", lines =>
        {
            lines.ToTable("reservation_lines");
            // The owner key is already named OrderId, so the shadow FK needs an explicit type.
            lines.Property<Guid>("OrderId").HasColumnName("order_id");
            lines.HasKey("OrderId", nameof(ReservationLine.ProductId));
            lines.Property(line => line.ProductId).ValueGeneratedNever().HasColumnName("product_id");
            lines.Property(line => line.Quantity).HasColumnName("quantity").IsRequired();
        });
    }
}
