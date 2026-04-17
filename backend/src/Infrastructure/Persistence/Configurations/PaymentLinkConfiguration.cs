using Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class PaymentLinkConfiguration : IEntityTypeConfiguration<PaymentLink>
{
    public void Configure(EntityTypeBuilder<PaymentLink> builder)
    {
        builder.ToTable("payment_links");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderId)
            .IsRequired();

        builder.Property(x => x.PreferenceId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.ExpectedAmount)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.ExpectedCurrency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.PreferenceId)
            .IsUnique();

        builder.HasIndex(x => x.OrderId);

        builder.HasOne<Domain.Checkout.Order>()
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
