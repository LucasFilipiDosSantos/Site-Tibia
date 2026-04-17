using Application.Checkout.Contracts;
using Domain.Checkout;

namespace Application.Checkout.Services;

public sealed class CartService
{
    private readonly ICartRepository _cartRepository;
    private readonly ICartProductAvailabilityGateway _availabilityGateway;

    public CartService(ICartRepository cartRepository, ICartProductAvailabilityGateway availabilityGateway)
    {
        _cartRepository = cartRepository;
        _availabilityGateway = availabilityGateway;
    }

    public async Task<CartResponse> AddItemAsync(AddCartItemRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCustomerId(request.CustomerId);
        ValidateProductId(request.ProductId);
        ValidateQuantity(request.Quantity);

        var cart = await _cartRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken)
            ?? new Cart(request.CustomerId);

        var existingQuantity = cart.Lines.SingleOrDefault(line => line.ProductId == request.ProductId)?.Quantity ?? 0;
        var finalQuantity = existingQuantity + request.Quantity;

        await EnsureAvailableAsync(request.ProductId, finalQuantity, cancellationToken);

        cart.AddOrMerge(request.ProductId, request.Quantity);
        await _cartRepository.SaveAsync(cart, cancellationToken);

        return ToResponse(cart);
    }

    public async Task<CartResponse> SetItemQuantityAsync(SetCartItemQuantityRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCustomerId(request.CustomerId);
        ValidateProductId(request.ProductId);
        ValidateQuantity(request.Quantity);

        var cart = await _cartRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException("Cart was not found for customer.");

        await EnsureAvailableAsync(request.ProductId, request.Quantity, cancellationToken);

        cart.SetQuantity(request.ProductId, request.Quantity);
        await _cartRepository.SaveAsync(cart, cancellationToken);

        return ToResponse(cart);
    }

    public async Task<CartResponse> RemoveItemAsync(RemoveCartItemRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCustomerId(request.CustomerId);
        ValidateProductId(request.ProductId);

        var cart = await _cartRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken)
            ?? new Cart(request.CustomerId);

        cart.Remove(request.ProductId);
        await _cartRepository.SaveAsync(cart, cancellationToken);

        return ToResponse(cart);
    }

    public async Task<CartResponse> GetCartAsync(GetCartRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCustomerId(request.CustomerId);

        var cart = await _cartRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken)
            ?? new Cart(request.CustomerId);

        return ToResponse(cart);
    }

    private async Task EnsureAvailableAsync(Guid productId, int requestedQuantity, CancellationToken cancellationToken)
    {
        var availability = await _availabilityGateway.GetAvailabilityAsync(productId, cancellationToken);
        if (requestedQuantity > availability.AvailableQuantity)
        {
            throw new CartStockConflictException(productId, requestedQuantity, availability.AvailableQuantity);
        }
    }

    private static CartResponse ToResponse(Cart cart)
    {
        return new CartResponse(
            cart.CustomerId,
            cart.Lines.Select(line => new CartLineResponse(line.ProductId, line.Quantity)).ToList());
    }

    private static void ValidateCustomerId(Guid customerId)
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("Customer id is required.", nameof(customerId));
        }
    }

    private static void ValidateProductId(Guid productId)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product id is required.", nameof(productId));
        }
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }
    }
}
