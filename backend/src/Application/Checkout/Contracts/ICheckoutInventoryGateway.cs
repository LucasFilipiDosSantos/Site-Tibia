using Application.Inventory.Contracts;

namespace Application.Checkout.Contracts;

public interface ICheckoutInventoryGateway
{
    Task ReserveStockForCheckoutAsync(
        Guid orderId,
        string orderIntentKey,
        Guid productId,
        int quantity,
        CancellationToken cancellationToken = default);

    Task ReleaseCheckoutReservationAsync(
        string orderIntentKey,
        ReservationReleaseReason reason,
        CancellationToken cancellationToken = default);
}
