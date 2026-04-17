using Domain.Checkout;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class OrderStatusTransitionEventConfiguration : IEntityTypeConfiguration<OrderStatusTransitionEvent>
{
    public void Configure(EntityTypeBuilder<OrderStatusTransitionEvent> builder)
    {
        builder.ToTable("order_status_transition_events");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderId)
            .IsRequired();

        builder.Property(x => x.FromStatus)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.ToStatus)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.SourceType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.OccurredAtUtc)
            .IsRequired();

        // Optional metadata per D-16
        builder.Property(x => x.ActorUserId)
            .IsRequired(false);

        builder.Property(x => x.Reason)
            .HasMaxLength(500)
            .IsRequired(false);

        // FK to orders (cascade delete already handled by order aggregate deletion)
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => new { x.OrderId, x.OccurredAtUtc });
    }
}