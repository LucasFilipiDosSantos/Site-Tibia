using Domain.Catalog;
using Domain.Checkout;
using Infrastructure.Persistence;
using Infrastructure.Checkout.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.Checkout;

[Trait("Category", "CheckoutCapture")]
[Trait("Requirement", "CHK-01")]
[Trait("Requirement", "CHK-02")]
[Trait("Requirement", "CHK-03")]
[Trait("Suite", "Phase04CheckoutPersistence")]
[Trait("Plan", "04-03")]
public sealed class CheckoutPersistenceContractTests
{
    [Fact]
    public async Task CartRepository_SaveAndLoad_PreservesMergedLineAndAbsoluteQuantity()
    {
        await using var fixture = await SqliteCheckoutFixture.CreateAsync();
        await using var db = fixture.CreateDbContext();
        var customerId = Guid.NewGuid();
        var productId = await fixture.SeedCatalogProductAsync("gold-pack", 10m);

        var repository = new CartRepository(db);
        var cart = new Cart(customerId);
        cart.AddOrMerge(productId, 1);
        cart.AddOrMerge(productId, 2);
        cart.SetQuantity(productId, 5);

        await repository.SaveAsync(cart);

        var loaded = await repository.GetByCustomerIdAsync(customerId);
        Assert.NotNull(loaded);
        var line = Assert.Single(loaded!.Lines);
        Assert.Equal(productId, line.ProductId);
        Assert.Equal(5, line.Quantity);
    }

    [Fact]
    public async Task CheckoutRepository_OrderSnapshotsRemainStoredAfterCatalogChanges()
    {
        await using var fixture = await SqliteCheckoutFixture.CreateAsync();
        await using var db = fixture.CreateDbContext();
        var customerId = Guid.NewGuid();
        var productId = await fixture.SeedCatalogProductAsync("starter-gold", 9.50m);

        var repository = new CheckoutRepository(db);
        var order = new Order(Guid.NewGuid(), customerId, "intent-checkout-1");
        order.AddItemSnapshot(new OrderItemSnapshot(productId, 2, 9.50m, "BRL", "Starter Gold", "starter-gold", "gold"));
        order.AddDeliveryInstruction(DeliveryInstruction.CreateAutomated(productId, "KnightX", "Aurera", "whatsapp:+5511999999999"));

        await repository.SaveOrderAsync(order);

        await fixture.MutateProductAsync(productId, "Mutated Name", 99m, "mutated-slug");

        var stored = await repository.GetOrderByIdAsync(order.Id);
        Assert.NotNull(stored);
        var item = Assert.Single(stored!.Items);
        Assert.Equal(9.50m, item.UnitPrice);
        Assert.Equal("BRL", item.Currency);
        Assert.Equal("Starter Gold", item.ProductName);
        Assert.Equal("starter-gold", item.ProductSlug);
        Assert.Equal("gold", item.CategorySlug);
    }

    [Fact]
    public async Task CheckoutRepository_PersistsDeliveryInstructionBranches()
    {
        await using var fixture = await SqliteCheckoutFixture.CreateAsync();
        await using var db = fixture.CreateDbContext();
        var customerId = Guid.NewGuid();
        var automatedProductId = await fixture.SeedCatalogProductAsync("auto-item", 3m);
        var manualProductId = await fixture.SeedCatalogProductAsync("manual-item", 7m);

        var repository = new CheckoutRepository(db);
        var order = new Order(Guid.NewGuid(), customerId, "intent-checkout-2");
        order.AddItemSnapshot(new OrderItemSnapshot(automatedProductId, 1, 3m, "BRL", "Auto Item", "auto-item", "gold"));
        order.AddItemSnapshot(new OrderItemSnapshot(manualProductId, 1, 7m, "BRL", "Manual Item", "manual-item", "gold"));
        order.AddDeliveryInstruction(DeliveryInstruction.CreateAutomated(automatedProductId, "CharA", "Aurera", "whatsapp:+5511000000001"));
        order.AddDeliveryInstruction(DeliveryInstruction.CreateManual(manualProductId, "Need VIP delivery", "@manual-contact"));

        await repository.SaveOrderAsync(order);

        var stored = await repository.GetOrderByIdAsync(order.Id);
        Assert.NotNull(stored);
        Assert.Equal(2, stored!.DeliveryInstructions.Count);

        var automated = stored.DeliveryInstructions.Single(x => x.ProductId == automatedProductId);
        Assert.Equal(FulfillmentType.Automated, automated.FulfillmentType);
        Assert.Equal("CharA", automated.TargetCharacter);
        Assert.Equal("Aurera", automated.TargetServer);
        Assert.Equal("whatsapp:+5511000000001", automated.DeliveryChannelOrContact);
        Assert.Null(automated.RequestBrief);
        Assert.Null(automated.ContactHandle);

        var manual = stored.DeliveryInstructions.Single(x => x.ProductId == manualProductId);
        Assert.Equal(FulfillmentType.Manual, manual.FulfillmentType);
        Assert.Equal("Need VIP delivery", manual.RequestBrief);
        Assert.Equal("@manual-contact", manual.ContactHandle);
        Assert.Null(manual.TargetCharacter);
        Assert.Null(manual.TargetServer);
    }

    [Fact]
    public async Task CartRepository_Clear_RemovesCustomerCartState()
    {
        await using var fixture = await SqliteCheckoutFixture.CreateAsync();
        await using var db = fixture.CreateDbContext();
        var customerId = Guid.NewGuid();
        var productId = await fixture.SeedCatalogProductAsync("clear-cart-item", 2m);

        var repository = new CartRepository(db);
        var cart = new Cart(customerId);
        cart.AddOrMerge(productId, 1);
        await repository.SaveAsync(cart);

        await repository.ClearAsync(customerId);

        var loaded = await repository.GetByCustomerIdAsync(customerId);
        Assert.Null(loaded);
    }

    private sealed class SqliteCheckoutFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;

        private SqliteCheckoutFixture(SqliteConnection connection, DbContextOptions<AppDbContext> options)
        {
            _connection = connection;
            _options = options;
        }

        public static async Task<SqliteCheckoutFixture> CreateAsync()
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

            return new SqliteCheckoutFixture(connection, options);
        }

        public AppDbContext CreateDbContext()
        {
            var context = new AppDbContext(_options);
            context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");
            return context;
        }

        public async Task<Guid> SeedCatalogProductAsync(string slug, decimal price)
        {
            await using var db = CreateDbContext();
            var category = await db.Categories.SingleOrDefaultAsync(x => x.Slug == "gold");
            if (category is null)
            {
                category = new Category("Gold", "gold", "Gold offers");
                db.Categories.Add(category);
                await db.SaveChangesAsync();
            }

            var product = new Product($"{slug} name", slug, $"{slug} description", price, category.Id, category.Slug);
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return product.Id;
        }

        public async Task MutateProductAsync(Guid productId, string newName, decimal newPrice, string newSlug)
        {
            await using var db = CreateDbContext();
            var product = await db.Products.SingleAsync(x => x.Id == productId);
            product.ReplaceDetails(newName, "mutated description", newPrice, product.CategoryId, product.CategorySlug);
            var slugProperty = typeof(Product).GetProperty(nameof(Product.Slug));
            slugProperty!.SetValue(product, newSlug);
            await db.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }
}
