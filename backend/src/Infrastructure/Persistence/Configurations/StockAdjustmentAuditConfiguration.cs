using Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class StockAdjustmentAuditConfiguration : IEntityTypeConfiguration<StockAdjustmentAudit>
{
    public void Configure(EntityTypeBuilder<StockAdjustmentAudit> builder)
    {
        builder.ToTable("stock_adjustment_audits");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.AdminUserId)
            .IsRequired();

        builder.Property(x => x.Delta)
            .IsRequired();

        builder.Property(x => x.BeforeQuantity)
            .IsRequired();

        builder.Property(x => x.AfterQuantity)
            .IsRequired();

        builder.Property(x => x.Reason)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(x => x.AdjustedAtUtc)
            .IsRequired();

        builder.HasOne<Domain.Catalog.Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
