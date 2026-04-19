using Application.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Infrastructure.Notifications;

/// <summary>
/// Implementation of NotificationOutboxRepository using EF Core.
/// D-04: Persists failed notifications for retry without rollback.
/// </summary>
public sealed class NotificationOutboxRepository : INotificationOutboxRepository
{
    private readonly NotificationDbContext _context;

    public NotificationOutboxRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(string idempotencyKey, CancellationToken ct = default)
    {
        return await _context.NotificationOutbox
            .AnyAsync(x => x.IdempotencyKey == idempotencyKey, ct);
    }

    public async Task SaveFailedAsync(NotificationOutboxEntry entry, CancellationToken ct = default)
    {
        _context.NotificationOutbox.Add(entry);
        await _context.SaveChangesAsync(ct);
    }
}

/// <summary>
/// DbContext for notification persistence (outbox/retry).
/// </summary>
public sealed class NotificationDbContext : DbContext
{
    public DbSet<NotificationOutboxEntry> NotificationOutbox { get; set; } = null!;

    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationOutboxEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.IdempotencyKey).IsUnique();
            entity.Property(e => e.IdempotencyKey).HasMaxLength(200);
            entity.Property(e => e.FailureReason).HasMaxLength(500);
        });
    }
}