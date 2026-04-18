using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuration for payment event dedupe entity (D-06)
/// </summary>
public sealed class PaymentEventDedupConfiguration : IEntityTypeConfiguration<PaymentEventDedupEntity>
{
    public void Configure(EntityTypeBuilder<PaymentEventDedupEntity> builder)
    {
        builder.ToTable("payment_event_dedup");
        
        builder.HasKey(e => e.Id);
        
        // D-06: Unique constraint on dedupe key
        builder.HasIndex(e => new { e.ProviderResourceId, e.Action }).IsUnique();
        
        builder.Property(e => e.ProviderResourceId)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(e => e.Action)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(e => e.ProcessedAtUtc)
            .IsRequired();
    }
}

/// <summary>
/// Entity for webhook dedupe guard (D-06)
/// </summary>
public sealed class PaymentEventDedupEntity
{
    public Guid Id { get; set; }
    public string ProviderResourceId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAtUtc { get; set; }
}