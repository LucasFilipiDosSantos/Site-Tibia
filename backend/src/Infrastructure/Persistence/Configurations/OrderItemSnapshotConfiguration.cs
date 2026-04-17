using Domain.Checkout;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class OrderItemSnapshotConfiguration : IEntityTypeConfiguration<OrderItemSnapshot>
{
    public void Configure(EntityTypeBuilder<OrderItemSnapshot> builder)
    {
        builder.ToTable("order_item_snapshots");

        builder.HasKey("OrderId", nameof(OrderItemSnapshot.ProductId));

        builder.Property<Guid>("OrderId")
            .IsRequired();

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.Property(x => x.UnitPrice)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.ProductName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.ProductSlug)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.CategorySlug)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasOne<Domain.Catalog.Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
