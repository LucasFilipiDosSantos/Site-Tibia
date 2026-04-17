using Application.Checkout.Contracts;
using Application.Inventory.Contracts;
using Domain.Checkout;

namespace UnitTests.Checkout;

public sealed class CheckoutServiceTests
{
    [Fact]
    public async Task SubmitCheckout_CreatesImmutableSnapshots()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = new Cart(customerId);
        cart.AddOrMerge(productId, 2);

        var cartRepository = new InMemoryCartRepository(cart);
        var checkoutRepository = new InMemoryCheckoutRepository();
        var inventoryGateway = new SuccessReserveGateway();
        var catalogGateway = new InMemoryCheckoutProductCatalogGateway(
            new CheckoutProductSnapshot(productId, "Gold Pack", "gold-pack", "gold", 12.50m, "BRL", FulfillmentType.Automated));

        var sut = CreateSut(cartRepository, checkoutRepository, inventoryGateway, catalogGateway);

        var response = await sut.SubmitCheckoutAsync(new SubmitCheckoutRequest(
            customerId,
            [new CheckoutDeliveryInstructionRequest(productId, "KnightX", "Aurera", "whatsapp:+5511999999999", null, null)]));

        var item = Assert.Single(response.Items);
        Assert.Equal(12.50m, item.UnitPrice);
        Assert.Equal("BRL", item.Currency);
        Assert.Equal("Gold Pack", item.ProductName);
        Assert.Equal("gold-pack", item.ProductSlug);
        Assert.Equal("gold", item.CategorySlug);
    }

    [Fact]
    public async Task SubmitCheckout_ValidatesDeliveryInstructionsByFulfillmentType()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = new Cart(customerId);
        cart.AddOrMerge(productId, 1);

        var cartRepository = new InMemoryCartRepository(cart);
        var checkoutRepository = new InMemoryCheckoutRepository();
        var inventoryGateway = new SuccessReserveGateway();
        var catalogGateway = new InMemoryCheckoutProductCatalogGateway(
            new CheckoutProductSnapshot(productId, "Manual Service", "manual-service", "services", 20m, "BRL", FulfillmentType.Manual));

        var sut = CreateSut(cartRepository, checkoutRepository, inventoryGateway, catalogGateway);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.SubmitCheckoutAsync(new SubmitCheckoutRequest(
                customerId,
                [new CheckoutDeliveryInstructionRequest(productId, null, null, null, "", "")])));
    }

    [Fact]
    public async Task SubmitCheckout_OnAnyReserveConflict_DoesNotPersistOrder()
    {
        var customerId = Guid.NewGuid();
        var lineA = Guid.NewGuid();
        var lineB = Guid.NewGuid();
        var cart = new Cart(customerId);
        cart.AddOrMerge(lineA, 1);
        cart.AddOrMerge(lineB, 3);

        var cartRepository = new InMemoryCartRepository(cart);
        var checkoutRepository = new InMemoryCheckoutRepository();
        var inventoryGateway = new ConflictReserveGateway(lineB, requestedQuantity: 3, availableQuantity: 1);
        var catalogGateway = new InMemoryCheckoutProductCatalogGateway(
            new CheckoutProductSnapshot(lineA, "A", "a", "cat", 1m, "BRL", FulfillmentType.Automated),
            new CheckoutProductSnapshot(lineB, "B", "b", "cat", 2m, "BRL", FulfillmentType.Automated));

        var sut = CreateSut(cartRepository, checkoutRepository, inventoryGateway, catalogGateway);

        var ex = await Assert.ThrowsAsync<CheckoutReservationConflictException>(() =>
            sut.SubmitCheckoutAsync(new SubmitCheckoutRequest(
                customerId,
                [
                    new CheckoutDeliveryInstructionRequest(lineA, "CharA", "Aurera", "chan-a", null, null),
                    new CheckoutDeliveryInstructionRequest(lineB, "CharB", "Aurera", "chan-b", null, null)
                ])));

        Assert.Single(ex.LineConflicts);
        Assert.Equal(lineB, ex.LineConflicts[0].ProductId);
        Assert.Equal(3, ex.LineConflicts[0].RequestedQuantity);
        Assert.Equal(1, ex.LineConflicts[0].AvailableQuantity);
        Assert.Equal(0, checkoutRepository.SavedOrders.Count);
        Assert.Equal(0, cartRepository.ClearCallCount);
    }

    [Fact]
    public async Task SubmitCheckout_OnSecondLineConflict_CompensatesPriorSuccessfulReservations()
    {
        var customerId = Guid.NewGuid();
        var lineA = Guid.NewGuid();
        var lineB = Guid.NewGuid();
        var cart = new Cart(customerId);
        cart.AddOrMerge(lineA, 1);
        cart.AddOrMerge(lineB, 2);

        var cartRepository = new InMemoryCartRepository(cart);
        var checkoutRepository = new InMemoryCheckoutRepository();
        var inventoryGateway = new TrackingReserveGateway(lineB, requestedQuantity: 2, availableQuantity: 1);
        var catalogGateway = new InMemoryCheckoutProductCatalogGateway(
            new CheckoutProductSnapshot(lineA, "A", "a", "cat", 1m, "BRL", FulfillmentType.Automated),
            new CheckoutProductSnapshot(lineB, "B", "b", "cat", 2m, "BRL", FulfillmentType.Automated));

        var sut = CreateSut(cartRepository, checkoutRepository, inventoryGateway, catalogGateway);

        await Assert.ThrowsAsync<CheckoutReservationConflictException>(() =>
            sut.SubmitCheckoutAsync(new SubmitCheckoutRequest(
                customerId,
                [
                    new CheckoutDeliveryInstructionRequest(lineA, "CharA", "Aurera", "chan-a", null, null),
                    new CheckoutDeliveryInstructionRequest(lineB, "CharB", "Aurera", "chan-b", null, null)
                ])));

        Assert.Equal(1, inventoryGateway.ReleaseCallCount);
        Assert.Equal(0, inventoryGateway.GetReservedQuantityForLastIntent(lineA));
        Assert.Equal(0, inventoryGateway.GetReservedQuantityForLastIntent(lineB));
    }

    [Fact]
    public async Task SubmitCheckout_WhenCompensationFails_ThrowsDeterministicExceptionAndDoesNotPersistOrder()
    {
        var customerId = Guid.NewGuid();
        var lineA = Guid.NewGuid();
        var lineB = Guid.NewGuid();
        var cart = new Cart(customerId);
        cart.AddOrMerge(lineA, 1);
        cart.AddOrMerge(lineB, 2);

        var cartRepository = new InMemoryCartRepository(cart);
        var checkoutRepository = new InMemoryCheckoutRepository();
        var inventoryGateway = new TrackingReserveGateway(lineB, requestedQuantity: 2, availableQuantity: 1)
        {
            ThrowOnRelease = true
        };
        var catalogGateway = new InMemoryCheckoutProductCatalogGateway(
            new CheckoutProductSnapshot(lineA, "A", "a", "cat", 1m, "BRL", FulfillmentType.Automated),
            new CheckoutProductSnapshot(lineB, "B", "b", "cat", 2m, "BRL", FulfillmentType.Automated));

        var sut = CreateSut(cartRepository, checkoutRepository, inventoryGateway, catalogGateway);

        var ex = await Assert.ThrowsAsync<CheckoutReservationCompensationException>(() =>
            sut.SubmitCheckoutAsync(new SubmitCheckoutRequest(
                customerId,
                [
                    new CheckoutDeliveryInstructionRequest(lineA, "CharA", "Aurera", "chan-a", null, null),
                    new CheckoutDeliveryInstructionRequest(lineB, "CharB", "Aurera", "chan-b", null, null)
                ])));

        Assert.Contains("compensation failed", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, checkoutRepository.SavedOrders.Count);
        Assert.Equal(0, cartRepository.ClearCallCount);
    }

    [Fact]
    public async Task SubmitCheckout_Success_ClearsCart()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = new Cart(customerId);
        cart.AddOrMerge(productId, 1);

        var cartRepository = new InMemoryCartRepository(cart);
        var checkoutRepository = new InMemoryCheckoutRepository();
        var inventoryGateway = new SuccessReserveGateway();
        var catalogGateway = new InMemoryCheckoutProductCatalogGateway(
            new CheckoutProductSnapshot(productId, "Fast Gold", "fast-gold", "gold", 5m, "BRL", FulfillmentType.Automated));

        var sut = CreateSut(cartRepository, checkoutRepository, inventoryGateway, catalogGateway);

        await sut.SubmitCheckoutAsync(new SubmitCheckoutRequest(
            customerId,
            [new CheckoutDeliveryInstructionRequest(productId, "Knight", "Aurera", "whatsapp:+5511888888888", null, null)]));

        Assert.Single(checkoutRepository.SavedOrders);
        Assert.Equal(1, cartRepository.ClearCallCount);
    }

    private static Application.Checkout.Services.CheckoutService CreateSut(
        ICartRepository cartRepository,
        ICheckoutRepository checkoutRepository,
        ICheckoutInventoryGateway inventoryGateway,
        ICheckoutProductCatalogGateway catalogGateway)
    {
        return new Application.Checkout.Services.CheckoutService(cartRepository, checkoutRepository, inventoryGateway, catalogGateway);
    }

    private sealed class InMemoryCartRepository : ICartRepository
    {
        private readonly Dictionary<Guid, Cart> _carts = [];

        public InMemoryCartRepository(Cart seeded)
        {
            _carts[seeded.CustomerId] = seeded;
        }

        public int ClearCallCount { get; private set; }

        public Task<Cart?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            _carts.TryGetValue(customerId, out var cart);
            return Task.FromResult(cart);
        }

        public Task SaveAsync(Cart cart, CancellationToken cancellationToken = default)
        {
            _carts[cart.CustomerId] = cart;
            return Task.CompletedTask;
        }

        public Task ClearAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            ClearCallCount++;
            _carts.Remove(customerId);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryCheckoutRepository : ICheckoutRepository
    {
        public List<Order> SavedOrders { get; } = [];

        public Task SaveOrderAsync(Order order, CancellationToken cancellationToken = default)
        {
            SavedOrders.Add(order);
            return Task.CompletedTask;
        }

        public Task<Order?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SavedOrders.SingleOrDefault(x => x.Id == orderId));
        }
    }

    private sealed class InMemoryCheckoutProductCatalogGateway : ICheckoutProductCatalogGateway
    {
        private readonly Dictionary<Guid, CheckoutProductSnapshot> _snapshots;

        public InMemoryCheckoutProductCatalogGateway(params CheckoutProductSnapshot[] snapshots)
        {
            _snapshots = snapshots.ToDictionary(x => x.ProductId);
        }

        public Task<CheckoutProductSnapshot> GetSnapshotAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_snapshots[productId]);
        }
    }

    private sealed class SuccessReserveGateway : ICheckoutInventoryGateway
    {
        public Task ReserveStockForCheckoutAsync(Guid orderId, string orderIntentKey, Guid productId, int quantity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task ReleaseCheckoutReservationAsync(string orderIntentKey, ReservationReleaseReason reason, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class ConflictReserveGateway : ICheckoutInventoryGateway
    {
        private readonly Guid _conflictingProductId;
        private readonly int _requestedQuantity;
        private readonly int _availableQuantity;

        public ConflictReserveGateway(Guid conflictingProductId, int requestedQuantity, int availableQuantity)
        {
            _conflictingProductId = conflictingProductId;
            _requestedQuantity = requestedQuantity;
            _availableQuantity = availableQuantity;
        }

        public Task ReserveStockForCheckoutAsync(Guid orderId, string orderIntentKey, Guid productId, int quantity, CancellationToken cancellationToken = default)
        {
            if (productId == _conflictingProductId)
            {
                throw new CheckoutReservationConflictException([new CheckoutLineConflict(productId, _requestedQuantity, _availableQuantity)]);
            }

            return Task.CompletedTask;
        }

        public Task ReleaseCheckoutReservationAsync(string orderIntentKey, ReservationReleaseReason reason, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TrackingReserveGateway : ICheckoutInventoryGateway
    {
        private readonly Guid _conflictingProductId;
        private readonly int _requestedQuantity;
        private readonly int _availableQuantity;
        private readonly Dictionary<string, Dictionary<Guid, int>> _reservedByIntent = [];

        public TrackingReserveGateway(Guid conflictingProductId, int requestedQuantity, int availableQuantity)
        {
            _conflictingProductId = conflictingProductId;
            _requestedQuantity = requestedQuantity;
            _availableQuantity = availableQuantity;
        }

        public int ReleaseCallCount { get; private set; }
        public string? LastOrderIntentKey { get; private set; }
        public bool ThrowOnRelease { get; set; }

        public Task ReserveStockForCheckoutAsync(Guid orderId, string orderIntentKey, Guid productId, int quantity, CancellationToken cancellationToken = default)
        {
            LastOrderIntentKey = orderIntentKey;
            if (productId == _conflictingProductId)
            {
                throw new CheckoutReservationConflictException([new CheckoutLineConflict(productId, _requestedQuantity, _availableQuantity)]);
            }

            if (!_reservedByIntent.TryGetValue(orderIntentKey, out var perProduct))
            {
                perProduct = [];
                _reservedByIntent[orderIntentKey] = perProduct;
            }

            perProduct[productId] = perProduct.TryGetValue(productId, out var existing)
                ? existing + quantity
                : quantity;

            return Task.CompletedTask;
        }

        public Task ReleaseCheckoutReservationAsync(string orderIntentKey, ReservationReleaseReason reason, CancellationToken cancellationToken = default)
        {
            ReleaseCallCount++;
            if (ThrowOnRelease)
            {
                throw new InvalidOperationException("simulated release failure");
            }

            _reservedByIntent[orderIntentKey] = [];
            return Task.CompletedTask;
        }

        public int GetReservedQuantityForLastIntent(Guid productId)
        {
            if (LastOrderIntentKey is null)
            {
                return 0;
            }

            return _reservedByIntent.TryGetValue(LastOrderIntentKey, out var perProduct)
                && perProduct.TryGetValue(productId, out var quantity)
                ? quantity
                : 0;
        }
    }
}
