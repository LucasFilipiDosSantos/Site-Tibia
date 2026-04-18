using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuration for payment webhook log entity
/// </summary>
public sealed class PaymentWebhookLogConfiguration : IEntityTypeConfiguration<PaymentWebhookLogEntity>
{
    public void Configure(EntityTypeBuilder<PaymentWebhookLogEntity> builder)
    {
        builder.ToTable("payment_webhook_logs");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.RequestId)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(e => e.Topic)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(e => e.Action)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(e => e.ProviderResourceId)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(e => e.ReceivedAtUtc)
            .IsRequired();
            
        builder.Property(e => e.ValidationOutcome)
            .IsRequired();
            
        builder.HasIndex(e => e.RequestId).IsUnique();
        builder.HasIndex(e => e.ProviderResourceId);
    }
}

/// <summary>
/// Entity for webhook log persistence
/// </summary>
public sealed class PaymentWebhookLogEntity
{
    public Guid Id { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ProviderResourceId { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public int ValidationOutcome { get; set; }
}