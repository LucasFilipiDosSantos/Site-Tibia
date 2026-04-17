using Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class InventoryStockConfiguration : IEntityTypeConfiguration<InventoryStock>
{
    public void Configure(EntityTypeBuilder<InventoryStock> builder)
    {
        builder.ToTable("inventory_stocks", table =>
        {
            table.HasCheckConstraint("CK_inventory_stocks_total_non_negative", "\"TotalQuantity\" >= 0");
            table.HasCheckConstraint("CK_inventory_stocks_reserved_non_negative", "\"ReservedQuantity\" >= 0");
            table.HasCheckConstraint("CK_inventory_stocks_reserved_le_total", "\"ReservedQuantity\" <= \"TotalQuantity\"");
        });

        builder.HasKey(x => x.ProductId);

        builder.Property(x => x.ProductId)
            .ValueGeneratedNever();

        builder.Property(x => x.TotalQuantity)
            .IsRequired();

        builder.Property(x => x.ReservedQuantity)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.Property(x => x.ConcurrencyVersion)
            .IsRequired()
            .IsConcurrencyToken();

        builder.Ignore(x => x.AvailableQuantity);

        builder.HasOne<Domain.Catalog.Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
