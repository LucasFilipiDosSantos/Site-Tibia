namespace Application.Checkout.Contracts;

public interface ICartProductAvailabilityGateway
{
    Task<ProductAvailabilityResponse> GetAvailabilityAsync(Guid productId, CancellationToken cancellationToken = default);
}
