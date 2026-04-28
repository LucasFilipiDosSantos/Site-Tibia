using Domain.Catalog;
using Domain.Checkout;
using Domain.Inventory;
using Application.Checkout.Contracts;
using Application.Checkout.Services;
using Application.Identity.Contracts;
using Application.Inventory.Contracts;
using Application.Inventory.Services;
using Infrastructure.Checkout;
using Infrastructure.Persistence;
using Infrastructure.Checkout.Repositories;
using Infrastructure.Identity.Repositories;
using Infrastructure.Inventory.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

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
    public async Task SubmitCheckout_MultiLineSuccess_ReservesAllLinesCreatesSingleOrderAndClearsCart()
    {
        await using var fixture = await SqliteCheckoutFixture.CreateAsync();
        await using var db = fixture.CreateDbContext();
        var now = new DateTimeOffset(2026, 4, 17, 15, 0, 0, TimeSpan.Zero);

        var customerId = Guid.NewGuid();
        var productA = await fixture.SeedCatalogProductAsync("bundle-a", 10m);
        var productB = await fixture.SeedCatalogProductAsync("bundle-b", 20m);
        await fixture.SeedUserAsync(customerId, "checkout-a@test.com");

        db.InventoryStocks.Add(new InventoryStock(productA, totalQuantity: 10, reservedQuantity: 0, now));
        db.InventoryStocks.Add(new InventoryStock(productB, totalQuantity: 10, reservedQuantity: 0, now));
        await db.SaveChangesAsync();

        var cartRepository = new CartRepository(db);
        var checkoutRepository = new CheckoutRepository(db);
        var inventoryRepository = new InventoryRepository(db);
        var inventoryService = new InventoryService(inventoryRepository, new FixedClock(now));
        var checkoutService = new CheckoutService(
            cartRepository,
            checkoutRepository,
            new CustomerRepository(db),
            new UserRepository(db),
            new CheckoutInventoryGateway(inventoryService),
            new CheckoutProductCatalogGateway(new Infrastructure.Catalog.Repositories.ProductRepository(db)),
            NullLogger<CheckoutService>.Instance);

        var cart = new Cart(customerId);
        cart.AddOrMerge(productA, 2);
        cart.AddOrMerge(productB, 3);
        await cartRepository.SaveAsync(cart);

        var response = await checkoutService.SubmitCheckoutAsync(
            new SubmitCheckoutRequest(
                customerId,
                [
                    new CheckoutDeliveryInstructionRequest(productA, "KnightA", "Aurera", "chan-a", null, null),
                    new CheckoutDeliveryInstructionRequest(productB, "KnightB", "Aurera", "chan-b", null, null)
                ]));

        Assert.Equal(2, response.Items.Count);
        Assert.Equal(1, await db.Orders.CountAsync());
        Assert.Equal(2, await db.OrderItemSnapshots.CountAsync());
        Assert.Equal(2, await db.DeliveryInstructions.CountAsync());
        Assert.Equal(2, await db.InventoryReservations.CountAsync(x => x.OrderIntentKey == response.OrderIntentKey && x.ReleasedAtUtc == null));

        var cartAfter = await cartRepository.GetByCustomerIdAsync(customerId);
        Assert.Null(cartAfter);

        var availabilityA = await inventoryService.GetAvailabilityAsync(new GetInventoryAvailabilityRequest(productA));
        var availabilityB = await inventoryService.GetAvailabilityAsync(new GetInventoryAvailabilityRequest(productB));
        Assert.Equal(2, availabilityA.Reserved);
        Assert.Equal(3, availabilityB.Reserved);
    }

    [Fact]
    public async Task SubmitCheckout_SecondLineConflict_ReleasesPriorReservationsPersistsNoOrderAndKeepsCart()
    {
        await using var fixture = await SqliteCheckoutFixture.CreateAsync();
        await using var db = fixture.CreateDbContext();
        var now = new DateTimeOffset(2026, 4, 17, 15, 0, 0, TimeSpan.Zero);

        var customerId = Guid.NewGuid();
        var productA = await fixture.SeedCatalogProductAsync("line-a", 10m);
        var productB = await fixture.SeedCatalogProductAsync("line-b", 20m);
        await fixture.SeedUserAsync(customerId, "checkout-b@test.com");

        db.InventoryStocks.Add(new InventoryStock(productA, totalQuantity: 5, reservedQuantity: 0, now));
        db.InventoryStocks.Add(new InventoryStock(productB, totalQuantity: 1, reservedQuantity: 0, now));
        await db.SaveChangesAsync();

        var cartRepository = new CartRepository(db);
        var checkoutRepository = new CheckoutRepository(db);
        var inventoryRepository = new InventoryRepository(db);
        var inventoryService = new InventoryService(inventoryRepository, new FixedClock(now));
        var checkoutService = new CheckoutService(
            cartRepository,
            checkoutRepository,
            new CustomerRepository(db),
            new UserRepository(db),
            new CheckoutInventoryGateway(inventoryService),
            new CheckoutProductCatalogGateway(new Infrastructure.Catalog.Repositories.ProductRepository(db)),
            NullLogger<CheckoutService>.Instance);

        var cart = new Cart(customerId);
        cart.AddOrMerge(productA, 1);
        cart.AddOrMerge(productB, 2);
        await cartRepository.SaveAsync(cart);

        var conflict = await Assert.ThrowsAsync<CheckoutReservationConflictException>(() =>
            checkoutService.SubmitCheckoutAsync(
                new SubmitCheckoutRequest(
                    customerId,
                    [
                        new CheckoutDeliveryInstructionRequest(productA, "KnightA", "Aurera", "chan-a", null, null),
                        new CheckoutDeliveryInstructionRequest(productB, "KnightB", "Aurera", "chan-b", null, null)
                    ])));

        Assert.Contains(conflict.LineConflicts, x => x.ProductId == productB && x.AvailableQuantity == 1);
        Assert.Equal(0, await db.Orders.CountAsync());
        Assert.Equal(0, await db.OrderItemSnapshots.CountAsync());

        var cartAfter = await cartRepository.GetByCustomerIdAsync(customerId);
        Assert.NotNull(cartAfter);
        Assert.Equal(2, cartAfter!.Lines.Count);

        var availabilityA = await inventoryService.GetAvailabilityAsync(new GetInventoryAvailabilityRequest(productA));
        var availabilityB = await inventoryService.GetAvailabilityAsync(new GetInventoryAvailabilityRequest(productB));
        Assert.Equal(0, availabilityA.Reserved);
        Assert.Equal(0, availabilityB.Reserved);

        var releasedForIntent = await db.InventoryReservations
            .Where(x => x.OrderIntentKey.StartsWith("checkout-"))
            .ToListAsync();
        Assert.All(releasedForIntent, reservation => Assert.NotNull(reservation.ReleasedAtUtc));
    }

    [Fact]
    public async Task InventoryRelease_ByIntent_ReleasesAllActiveRowsRestoresAllStocksAndIsIdempotent()
    {
        await using var fixture = await SqliteCheckoutFixture.CreateAsync();
        await using var db = fixture.CreateDbContext();
        var now = new DateTimeOffset(2026, 4, 17, 15, 0, 0, TimeSpan.Zero);

        var orderId = Guid.NewGuid();
        const string orderIntentKey = "checkout-release-all-intent";
        var productA = await fixture.SeedCatalogProductAsync("release-a", 10m);
        var productB = await fixture.SeedCatalogProductAsync("release-b", 20m);

        db.InventoryStocks.Add(new InventoryStock(productA, totalQuantity: 10, reservedQuantity: 0, now));
        db.InventoryStocks.Add(new InventoryStock(productB, totalQuantity: 10, reservedQuantity: 0, now));
        await db.SaveChangesAsync();

        var inventoryRepository = new InventoryRepository(db);
        var inventoryService = new InventoryService(inventoryRepository, new FixedClock(now));

        await inventoryService.ReserveStockForCheckoutAsync(new ReserveStockForCheckoutRequest(orderIntentKey, orderId, productA, 2));
        await inventoryService.ReserveStockForCheckoutAsync(new ReserveStockForCheckoutRequest(orderIntentKey, orderId, productB, 3));

        var released = await inventoryService.ReleaseReservationAsync(
            new ReleaseReservationRequest(orderIntentKey, ReservationReleaseReason.OrderCanceled));

        Assert.Equal(5, released.ReleasedQuantity);

        var reservations = await db.InventoryReservations
            .Where(x => x.OrderIntentKey == orderIntentKey)
            .ToListAsync();
        Assert.Equal(2, reservations.Count);
        Assert.All(reservations, reservation => Assert.NotNull(reservation.ReleasedAtUtc));

        var availabilityA = await inventoryService.GetAvailabilityAsync(new GetInventoryAvailabilityRequest(productA));
        var availabilityB = await inventoryService.GetAvailabilityAsync(new GetInventoryAvailabilityRequest(productB));
        Assert.Equal(0, availabilityA.Reserved);
        Assert.Equal(0, availabilityB.Reserved);
        Assert.Equal(10, availabilityA.Available);
        Assert.Equal(10, availabilityB.Available);

        var releasedAgain = await inventoryService.ReleaseReservationAsync(
            new ReleaseReservationRequest(orderIntentKey, ReservationReleaseReason.OrderCanceled));

        Assert.Equal(0, releasedAgain.ReleasedQuantity);

        var availabilityAfterSecondReleaseA = await inventoryService.GetAvailabilityAsync(new GetInventoryAvailabilityRequest(productA));
        var availabilityAfterSecondReleaseB = await inventoryService.GetAvailabilityAsync(new GetInventoryAvailabilityRequest(productB));
        Assert.Equal(0, availabilityAfterSecondReleaseA.Reserved);
        Assert.Equal(0, availabilityAfterSecondReleaseB.Reserved);
        Assert.Equal(10, availabilityAfterSecondReleaseA.Available);
        Assert.Equal(10, availabilityAfterSecondReleaseB.Available);
    }

    [Fact]
    public async Task SubmitCheckout_ThirdLineConflict_ReleasesAllPriorReservationsPersistsNoOrderAndLeavesNoResidualReserved()
    {
        await using var fixture = await SqliteCheckoutFixture.CreateAsync();
        await using var db = fixture.CreateDbContext();
        var now = new DateTimeOffset(2026, 4, 17, 15, 0, 0, TimeSpan.Zero);

        var customerId = Guid.NewGuid();
        var productA = await fixture.SeedCatalogProductAsync("line-3a", 10m);
        var productB = await fixture.SeedCatalogProductAsync("line-3b", 20m);
        var productC = await fixture.SeedCatalogProductAsync("line-3c", 30m);
        await fixture.SeedUserAsync(customerId, "checkout-c@test.com");

        db.InventoryStocks.Add(new InventoryStock(productA, totalQuantity: 5, reservedQuantity: 0, now));
        db.InventoryStocks.Add(new InventoryStock(productB, totalQuantity: 5, reservedQuantity: 0, now));
        db.InventoryStocks.Add(new InventoryStock(productC, totalQuantity: 1, reservedQuantity: 0, now));
        await db.SaveChangesAsync();

        var cartRepository = new CartRepository(db);
        var checkoutRepository = new CheckoutRepository(db);
        var inventoryRepository = new InventoryRepository(db);
        var inventoryService = new InventoryService(inventoryRepository, new FixedClock(now));
        var checkoutService = new CheckoutService(
            cartRepository,
            checkoutRepository,
            new CustomerRepository(db),
            new UserRepository(db),
            new CheckoutInventoryGateway(inventoryService),
            new CheckoutProductCatalogGateway(new Infrastructure.Catalog.Repositories.ProductRepository(db)),
            NullLogger<CheckoutService>.Instance);

        var cart = new Cart(customerId);
        cart.AddOrMerge(productA, 1);
        cart.AddOrMerge(productB, 2);
        cart.AddOrMerge(productC, 2);
        await cartRepository.SaveAsync(cart);

        var conflict = await Assert.ThrowsAsync<CheckoutReservationConflictException>(() =>
            checkoutService.SubmitCheckoutAsync(
                new SubmitCheckoutRequest(
                    customerId,
                    [
                        new CheckoutDeliveryInstructionRequest(productA, "KnightA", "Aurera", "chan-a", null, null),
                        new CheckoutDeliveryInstructionRequest(productB, "KnightB", "Aurera", "chan-b", null, null),
                        new CheckoutDeliveryInstructionRequest(productC, "KnightC", "Aurera", "chan-c", null, null)
                    ])));

        Assert.Contains(conflict.LineConflicts, x => x.ProductId == productC && x.AvailableQuantity == 1);

        Assert.Equal(0, await db.Orders.CountAsync());
        Assert.Equal(0, await db.OrderItemSnapshots.CountAsync());

        var cartAfter = await cartRepository.GetByCustomerIdAsync(customerId);
        Assert.NotNull(cartAfter);
        Assert.Equal(3, cartAfter!.Lines.Count);

        var availabilityA = await inventoryService.GetAvailabilityAsync(new GetInventoryAvailabilityRequest(productA));
        var availabilityB = await inventoryService.GetAvailabilityAsync(new GetInventoryAvailabilityRequest(productB));
        var availabilityC = await inventoryService.GetAvailabilityAsync(new GetInventoryAvailabilityRequest(productC));
        Assert.Equal(0, availabilityA.Reserved);
        Assert.Equal(0, availabilityB.Reserved);
        Assert.Equal(0, availabilityC.Reserved);

        var releasedReservations = await db.InventoryReservations
            .Where(x => x.OrderIntentKey.StartsWith("checkout-"))
            .ToListAsync();
        Assert.Equal(2, releasedReservations.Count);
        Assert.All(releasedReservations, reservation => Assert.NotNull(reservation.ReleasedAtUtc));
    }

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

            var product = new Product($"{slug} name", slug, $"{slug} description", price, category.Id, category.Slug, "Antica");
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return product.Id;
        }

        public async Task SeedUserAsync(Guid userId, string email, string name = "Checkout User")
        {
            await using var db = CreateDbContext();
            if (await db.Users.AnyAsync(x => x.Id == userId))
            {
                return;
            }

            db.Users.Add(new Domain.Identity.UserAccount(userId, name, email, "hash"));
            await db.SaveChangesAsync();
        }

        public async Task MutateProductAsync(Guid productId, string newName, decimal newPrice, string newSlug)
        {
            await using var db = CreateDbContext();
            var product = await db.Products.SingleAsync(x => x.Id == productId);
            product.ReplaceDetails(newName, "mutated description", newPrice, product.CategoryId, product.CategorySlug, product.Server, product.ImageUrl);
            var slugProperty = typeof(Product).GetProperty(nameof(Product.Slug));
            slugProperty!.SetValue(product, newSlug);
            await db.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }

    private sealed class FixedClock : ISystemClock
    {
        public FixedClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }
}
