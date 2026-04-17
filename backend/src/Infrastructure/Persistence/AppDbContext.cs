using Domain.Catalog;
using Domain.Identity;
using Domain.Inventory;
using Microsoft.EntityFrameworkCore;

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
    public DbSet<InventoryStock> InventoryStocks => Set<InventoryStock>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
    public DbSet<StockAdjustmentAudit> StockAdjustmentAudits => Set<StockAdjustmentAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresEnum<UserRole>();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
