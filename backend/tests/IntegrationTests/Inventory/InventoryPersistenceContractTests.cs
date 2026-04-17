using Application.Inventory.Contracts;
using Domain.Catalog;
using Domain.Inventory;
using Infrastructure.Inventory.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.Inventory;

[Trait("Category", "InventoryIntegrity")]
[Trait("Requirement", "INV-01")]
[Trait("Requirement", "INV-02")]
[Trait("Requirement", "INV-04")]
[Trait("Suite", "Phase03InventoryPersistence")]
[Trait("Plan", "03-02")]
public sealed class InventoryPersistenceContractTests
{
    [Fact]
    public async Task TryReserveAsync_SucceedsOnlyWhenAvailableAtLeastRequested()
    {
        await using var fixture = await SqliteInventoryFixture.CreateAsync();
        await using var db = fixture.CreateDbContext();

        var productId = await fixture.SeedCatalogAndProductAsync();
        db.InventoryStocks.Add(new InventoryStock(productId, totalQuantity: 5, reservedQuantity: 2, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        var repository = new InventoryRepository(db);
        var now = DateTimeOffset.UtcNow;
        var okAttempt = new ReserveInventoryAttempt("intent-ok", Guid.NewGuid(), productId, 3, now, now.AddMinutes(15));
        var blockedAttempt = new ReserveInventoryAttempt("intent-blocked", Guid.NewGuid(), productId, 1, now, now.AddMinutes(15));

        var ok = await repository.TryReserveAsync(okAttempt);
        var blocked = await repository.TryReserveAsync(blockedAttempt);

        Assert.True(ok.Success);
        Assert.False(blocked.Success);
    }

    [Fact]
    public async Task TryReserveAsync_DuplicateIntentKey_DoesNotCreateSecondReservationRow()
    {
        await using var fixture = await SqliteInventoryFixture.CreateAsync();
        await using var db = fixture.CreateDbContext();

        var productId = await fixture.SeedCatalogAndProductAsync();
        db.InventoryStocks.Add(new InventoryStock(productId, totalQuantity: 10, reservedQuantity: 0, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        var repository = new InventoryRepository(db);
        var now = DateTimeOffset.UtcNow;
        var attempt = new ReserveInventoryAttempt("intent-dup", Guid.NewGuid(), productId, 2, now, now.AddMinutes(15));

        await repository.TryReserveAsync(attempt);
        await repository.TryReserveAsync(attempt);

        var rows = await db.InventoryReservations.CountAsync(x => x.OrderIntentKey == "intent-dup" && x.ProductId == productId);
        Assert.Equal(1, rows);
    }

    [Fact]
    public async Task AdjustStockAsync_ConcurrencyConflict_RetriesAndPersistsAudit()
    {
        await using var fixture = await SqliteInventoryFixture.CreateAsync();
        await using var db = fixture.CreateDbContext();

        var productId = await fixture.SeedCatalogAndProductAsync();
        db.InventoryStocks.Add(new InventoryStock(productId, totalQuantity: 5, reservedQuantity: 0, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        var repository = new InventoryRepository(db);
        var command = new StockAdjustmentCommand(
            ProductId: productId,
            Delta: 2,
            BeforeQuantity: 5,
            AfterQuantity: 7,
            Reason: "manual reconciliation",
            AdminUserId: Guid.NewGuid(),
            AdjustedAtUtc: DateTimeOffset.UtcNow);

        var result = await repository.AdjustStockAsync(command);

        Assert.Equal(7, result.AfterQuantity);

        var audit = await db.StockAdjustmentAudits.SingleAsync();
        Assert.Equal(command.AdminUserId, audit.AdminUserId);
        Assert.Equal(command.ProductId, audit.ProductId);
        Assert.Equal(command.Delta, audit.Delta);
        Assert.Equal(command.BeforeQuantity, audit.BeforeQuantity);
        Assert.Equal(command.AfterQuantity, audit.AfterQuantity);
        Assert.Equal(command.Reason, audit.Reason);
        Assert.Equal(command.AdjustedAtUtc, audit.AdjustedAtUtc);
    }

    private sealed class SqliteInventoryFixture : IAsyncDisposable
    {
        private readonly DbContextOptions<AppDbContext> _options;

        private SqliteInventoryFixture(DbContextOptions<AppDbContext> options)
        {
            _options = options;
        }

        public static async Task<SqliteInventoryFixture> CreateAsync()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={Path.Combine(Path.GetTempPath(), $"inv-{Guid.NewGuid():N}.db")}")
                .Options;

            await using var setup = new AppDbContext(options);
            await setup.Database.EnsureCreatedAsync();

            return new SqliteInventoryFixture(options);
        }

        public AppDbContext CreateDbContext() => new(_options);

        public async Task<Guid> SeedCatalogAndProductAsync()
        {
            await using var db = CreateDbContext();
            var category = new Category("Gold", "gold", "Gold offers");
            var product = new Product("Gold Starter", "gold-starter", "Starter", 5m, category.Id, category.Slug);
            db.Categories.Add(category);
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return product.Id;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
