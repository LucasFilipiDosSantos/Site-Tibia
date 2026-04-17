namespace Application.Checkout.Contracts;

public sealed record AddCartItemRequest(Guid CustomerId, Guid ProductId, int Quantity);

public sealed record SetCartItemQuantityRequest(Guid CustomerId, Guid ProductId, int Quantity);

public sealed record RemoveCartItemRequest(Guid CustomerId, Guid ProductId);

public sealed record GetCartRequest(Guid CustomerId);

public sealed record CartLineResponse(Guid ProductId, int Quantity);

public sealed record CartResponse(Guid CustomerId, IReadOnlyList<CartLineResponse> Lines);

public sealed record ProductAvailabilityResponse(Guid ProductId, int AvailableQuantity);

public sealed class CartStockConflictException : InvalidOperationException
{
    public CartStockConflictException(Guid productId, int requestedQuantity, int availableQuantity)
        : base($"Requested quantity {requestedQuantity} exceeds available stock ({availableQuantity}) for product '{productId}'.")
    {
        ProductId = productId;
        RequestedQuantity = requestedQuantity;
        AvailableQuantity = availableQuantity;
    }

    public Guid ProductId { get; }

    public int RequestedQuantity { get; }

    public int AvailableQuantity { get; }
}
