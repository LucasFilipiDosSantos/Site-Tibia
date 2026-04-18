using Domain.Checkout;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class CustomRequestConfiguration : IEntityTypeConfiguration<CustomRequest>
{
    public void Configure(EntityTypeBuilder<CustomRequest> builder)
    {
        builder.ToTable("custom_requests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.OrderId)
            .HasColumnName("order_id");

        builder.Property(x => x.CustomerId)
            .IsRequired()
            .HasColumnName("customer_id");

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(4000)
            .HasColumnName("description");

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasColumnName("status");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc");

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasIndex(x => x.CustomerId);
    }
}