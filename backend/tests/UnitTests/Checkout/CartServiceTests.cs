using Application.Checkout.Contracts;
using Domain.Checkout;

namespace UnitTests.Checkout;

public sealed class CartServiceTests
{
    [Fact]
    public async Task AddItem_MergesExistingLine()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var repository = new InMemoryCartRepository(new Cart(customerId));
        var availability = new InMemoryAvailabilityGateway(availableQuantity: 10);

        var sut = CreateSut(repository, availability);

        await sut.AddItemAsync(new AddCartItemRequest(customerId, productId, 2));
        await sut.AddItemAsync(new AddCartItemRequest(customerId, productId, 3));

        var cart = await sut.GetCartAsync(new GetCartRequest(customerId));

        Assert.Single(cart.Lines);
        Assert.Equal(5, cart.Lines[0].Quantity);
    }

    [Fact]
    public async Task SetItemQuantity_SetQuantityUsesAbsoluteValue()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var seeded = new Cart(customerId);
        seeded.AddOrMerge(productId, 7);

        var repository = new InMemoryCartRepository(seeded);
        var availability = new InMemoryAvailabilityGateway(availableQuantity: 12);

        var sut = CreateSut(repository, availability);

        await sut.SetItemQuantityAsync(new SetCartItemQuantityRequest(customerId, productId, 4));

        var cart = await sut.GetCartAsync(new GetCartRequest(customerId));

        Assert.Single(cart.Lines);
        Assert.Equal(4, cart.Lines[0].Quantity);
    }

    [Fact]
    public async Task RemoveItem_RemoveUsesExplicitCommand()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var otherProductId = Guid.NewGuid();
        var seeded = new Cart(customerId);
        seeded.AddOrMerge(productId, 2);
        seeded.AddOrMerge(otherProductId, 1);

        var repository = new InMemoryCartRepository(seeded);
        var availability = new InMemoryAvailabilityGateway(availableQuantity: 10);

        var sut = CreateSut(repository, availability);

        await sut.RemoveItemAsync(new RemoveCartItemRequest(customerId, productId));

        var cart = await sut.GetCartAsync(new GetCartRequest(customerId));

        Assert.Single(cart.Lines);
        Assert.Equal(otherProductId, cart.Lines[0].ProductId);
    }

    [Fact]
    public async Task AddItem_ExceedsStockThrowsConflict()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var repository = new InMemoryCartRepository(new Cart(customerId));
        var availability = new InMemoryAvailabilityGateway(availableQuantity: 2);

        var sut = CreateSut(repository, availability);

        var ex = await Assert.ThrowsAsync<CartStockConflictException>(() =>
            sut.AddItemAsync(new AddCartItemRequest(customerId, productId, 3)));

        Assert.Equal(productId, ex.ProductId);
        Assert.Equal(3, ex.RequestedQuantity);
        Assert.Equal(2, ex.AvailableQuantity);
    }

    [Fact]
    public async Task SetItemQuantity_ExceedsStockThrowsConflict()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var seeded = new Cart(customerId);
        seeded.AddOrMerge(productId, 1);

        var repository = new InMemoryCartRepository(seeded);
        var availability = new InMemoryAvailabilityGateway(availableQuantity: 1);

        var sut = CreateSut(repository, availability);

        var ex = await Assert.ThrowsAsync<CartStockConflictException>(() =>
            sut.SetItemQuantityAsync(new SetCartItemQuantityRequest(customerId, productId, 5)));

        Assert.Equal(productId, ex.ProductId);
        Assert.Equal(5, ex.RequestedQuantity);
        Assert.Equal(1, ex.AvailableQuantity);
    }

    private static Application.Checkout.Services.CartService CreateSut(
        ICartRepository repository,
        ICartProductAvailabilityGateway availabilityGateway)
    {
        return new Application.Checkout.Services.CartService(repository, availabilityGateway);
    }

    private sealed class InMemoryAvailabilityGateway : ICartProductAvailabilityGateway
    {
        private readonly int _availableQuantity;

        public InMemoryAvailabilityGateway(int availableQuantity)
        {
            _availableQuantity = availableQuantity;
        }

        public Task<ProductAvailabilityResponse> GetAvailabilityAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ProductAvailabilityResponse(productId, _availableQuantity));
        }
    }

    private sealed class InMemoryCartRepository : ICartRepository
    {
        private readonly Dictionary<Guid, Cart> _carts = [];

        public InMemoryCartRepository(Cart seed)
        {
            _carts[seed.CustomerId] = seed;
        }

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
            _carts.Remove(customerId);
            return Task.CompletedTask;
        }
    }
}
