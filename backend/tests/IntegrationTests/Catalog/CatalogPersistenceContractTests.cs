using Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.Catalog;

[Trait("Category", "CatalogGovernance")]
[Trait("Requirement", "CAT-03")]
[Trait("Requirement", "CAT-04")]
[Trait("Suite", "Phase02CatalogPersistence")]
[Trait("Plan", "02-02")]
public sealed class CatalogPersistenceContractTests
{
    [Fact]
    public async Task ProductSlug_UniqueConstraint_IsEnforcedGlobally()
    {
        await using var fixture = await SqliteCatalogFixture.CreateAsync();
        await using var db = fixture.CreateDbContext();

        var category = new Domain.Catalog.Category("Gold", "gold", "Gold offers");
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        db.Products.Add(new Domain.Catalog.Product("Gold Starter", "starter", "Starter pack", 5m, category.Id, category.Slug));
        await db.SaveChangesAsync();

        db.Products.Add(new Domain.Catalog.Product("Gold Plus", "starter", "Plus pack", 10m, category.Id, category.Slug));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task CategorySlug_UniqueConstraint_IsEnforced()
    {
        await using var fixture = await SqliteCatalogFixture.CreateAsync();
        await using var db = fixture.CreateDbContext();

        db.Categories.Add(new Domain.Catalog.Category("Gold", "gold", "Gold offers"));
        await db.SaveChangesAsync();

        db.Categories.Add(new Domain.Catalog.Category("Gold Again", "gold", "Duplicate slug"));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task CategoryDelete_WithLinkedProducts_FailsDueToRestrictiveForeignKey()
    {
        await using var fixture = await SqliteCatalogFixture.CreateAsync();
        await using var db = fixture.CreateDbContext();

        var category = new Domain.Catalog.Category("Items", "items", "Item offers");
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        db.Products.Add(new Domain.Catalog.Product("Magic Sword", "magic-sword", "Sword", 15m, category.Id, category.Slug));
        await db.SaveChangesAsync();

        await using var deleteAttemptDb = fixture.CreateDbContext();
        var persistedCategory = await deleteAttemptDb.Categories.SingleAsync(x => x.Id == category.Id);
        deleteAttemptDb.Categories.Remove(persistedCategory);

        await Assert.ThrowsAsync<DbUpdateException>(() => deleteAttemptDb.SaveChangesAsync());
    }

    [Fact]
    public async Task ProductRepository_ListAsync_SupportsCombinedFiltersAndPagination()
    {
        await using var fixture = await SqliteCatalogFixture.CreateAsync();
        await using var db = fixture.CreateDbContext();

        var gold = new Domain.Catalog.Category("Gold", "gold", "Gold offers");
        var items = new Domain.Catalog.Category("Items", "items", "Item offers");
        db.Categories.AddRange(gold, items);
        await db.SaveChangesAsync();

        db.Products.AddRange(
            new Domain.Catalog.Product("Gold Starter", "gold-starter", "Starter", 5m, gold.Id, gold.Slug),
            new Domain.Catalog.Product("Gold Pro", "gold-pro", "Pro", 10m, gold.Id, gold.Slug),
            new Domain.Catalog.Product("Magic Sword", "magic-sword", "Sword", 20m, items.Id, items.Slug));
        await db.SaveChangesAsync();

        var repository = new Infrastructure.Catalog.Repositories.ProductRepository(db);
        var onlyGoldPro = await repository.ListAsync(new Application.Catalog.Contracts.ProductListQuery("gold", "gold-pro", 0, 10));
        var pagedGold = await repository.ListAsync(new Application.Catalog.Contracts.ProductListQuery("gold", null, 1, 1));

        var exact = Assert.Single(onlyGoldPro);
        Assert.Equal("gold-pro", exact.Slug);
        Assert.Equal("gold", exact.CategorySlug);

        var pageItem = Assert.Single(pagedGold);
        Assert.Equal("gold", pageItem.CategorySlug);
        Assert.Contains(pageItem.Slug, new[] { "gold-starter", "gold-pro" });
    }

    private sealed class SqliteCatalogFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;

        private SqliteCatalogFixture(SqliteConnection connection, DbContextOptions<AppDbContext> options)
        {
            _connection = connection;
            _options = options;
        }

        public static async Task<SqliteCatalogFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            await using (var setup = new AppDbContext(options))
            {
                await setup.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
                await setup.Database.EnsureCreatedAsync();
            }

            return new SqliteCatalogFixture(connection, options);
        }

        public AppDbContext CreateDbContext()
        {
            var context = new AppDbContext(_options);
            context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");
            return context;
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }
}
