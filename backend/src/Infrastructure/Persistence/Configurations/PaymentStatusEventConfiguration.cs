using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuration for payment status event entity (D-06)
/// </summary>
public sealed class PaymentStatusEventConfiguration : IEntityTypeConfiguration<PaymentStatusEventEntity>
{
    public void Configure(EntityTypeBuilder<PaymentStatusEventEntity> builder)
    {
        builder.ToTable("payment_status_events");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.ProviderResourceId)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(e => e.Action)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(e => e.ReceivedAtUtc)
            .IsRequired();
            
        builder.HasIndex(e => e.ProviderResourceId);
        builder.HasIndex(e => e.OrderId);
    }
}

/// <summary>
/// Entity for normalized payment status events
/// </summary>
public sealed class PaymentStatusEventEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string ProviderResourceId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public string? FailureReason { get; set; }
}