using Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.ToTable("inventory_reservations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderIntentKey)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.OrderId)
            .IsRequired();

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.Property(x => x.ReservedAtUtc)
            .IsRequired();

        builder.Property(x => x.ReservationExpiresAtUtc)
            .IsRequired();

        builder.Property(x => x.ReleasedAtUtc);

        builder.Property(x => x.ReleaseReason)
            .HasMaxLength(64);

        builder.HasIndex(x => new { x.OrderIntentKey, x.ProductId })
            .IsUnique();

        builder.HasOne<Domain.Catalog.Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
