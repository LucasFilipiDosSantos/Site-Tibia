using Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for AuditLog
/// </summary>
public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ActorId)
            .IsRequired();

        builder.Property(e => e.ActorEmail)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.EntityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.EntityId)
            .IsRequired();

        builder.Property(e => e.BeforeValue)
            .HasColumnType("jsonb");

        builder.Property(e => e.AfterValue)
            .HasColumnType("jsonb");

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired();

        builder.Property(e => e.IpAddress)
            .HasMaxLength(45);

        builder.HasIndex(e => e.CreatedAtUtc);
        builder.HasIndex(e => e.ActorId);
        builder.HasIndex(e => e.EntityId);
        builder.HasIndex(e => e.Action);
        builder.HasIndex(e => e.EntityType);
    }
}