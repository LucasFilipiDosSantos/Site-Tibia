using Domain.Checkout;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class DeliveryInstructionConfiguration : IEntityTypeConfiguration<DeliveryInstruction>
{
    public void Configure(EntityTypeBuilder<DeliveryInstruction> builder)
    {
        builder.ToTable("delivery_instructions");

        builder.HasKey("OrderId", nameof(DeliveryInstruction.ProductId));

        builder.Property<Guid>("OrderId")
            .IsRequired();

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.FulfillmentType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(DeliveryStatus.Pending);

        builder.Property(x => x.CompletedAtUtc)
            .HasColumnName("completed_at_utc");

        builder.Property(x => x.FailureReason)
            .HasMaxLength(500)
            .HasColumnName("failure_reason");

        builder.HasIndex(x => x.Status);

        builder.Property(x => x.TargetCharacter)
            .HasMaxLength(128);

        builder.Property(x => x.TargetServer)
            .HasMaxLength(128);

        builder.Property(x => x.DeliveryChannelOrContact)
            .HasMaxLength(256);

        builder.Property(x => x.RequestBrief)
            .HasMaxLength(2048);

        builder.Property(x => x.ContactHandle)
            .HasMaxLength(256);

        builder.HasOne<Domain.Catalog.Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}