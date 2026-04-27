using Domain.Audit;
using Domain.Catalog;
using Domain.Checkout;
using Domain.Identity;
using Domain.Inventory;
using Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence.Configurations;

namespace Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<RefreshSession> RefreshSessions => Set<RefreshSession>();
    public DbSet<SecurityToken> SecurityTokens => Set<SecurityToken>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<InventoryStock> InventoryStocks => Set<InventoryStock>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
    public DbSet<StockAdjustmentAudit> StockAdjustmentAudits => Set<StockAdjustmentAudit>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartLine> CartLines => Set<CartLine>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItemSnapshot> OrderItemSnapshots => Set<OrderItemSnapshot>();
    public DbSet<DeliveryInstruction> DeliveryInstructions => Set<DeliveryInstruction>();
    public DbSet<OrderStatusTransitionEvent> OrderStatusTransitionEvents => Set<OrderStatusTransitionEvent>();
    public DbSet<PaymentLink> PaymentLinks => Set<PaymentLink>();
    public DbSet<PaymentWebhookLogEntity> PaymentWebhookLogs => Set<PaymentWebhookLogEntity>();
    public DbSet<PaymentStatusEventEntity> PaymentStatusEvents => Set<PaymentStatusEventEntity>();
    public DbSet<PaymentEventDedupEntity> PaymentEventDedups => Set<PaymentEventDedupEntity>();
    public DbSet<CustomRequest> CustomRequests => Set<CustomRequest>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresEnum<UserRole>();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
