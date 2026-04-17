using Application.Checkout.Contracts;

namespace Application.Checkout.Services;

public sealed class CartService
{
    public CartService(ICartRepository cartRepository, ICartProductAvailabilityGateway availabilityGateway)
    {
    }

    public Task<CartResponse> AddItemAsync(AddCartItemRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<CartResponse> SetItemQuantityAsync(SetCartItemQuantityRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<CartResponse> RemoveItemAsync(RemoveCartItemRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<CartResponse> GetCartAsync(GetCartRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
