namespace Application.Checkout.Contracts;

public interface ICheckoutProductCatalogGateway
{
    Task<CheckoutProductSnapshot> GetSnapshotAsync(Guid productId, CancellationToken cancellationToken = default);
}
