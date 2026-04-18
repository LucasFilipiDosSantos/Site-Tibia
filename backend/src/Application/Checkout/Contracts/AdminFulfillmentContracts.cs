namespace Application.Checkout.Contracts;

public interface IAdminFulfillmentService
{
    Task ForceCompleteAsync(Guid orderId, Guid productId, string adminNote, CancellationToken cancellationToken);
}