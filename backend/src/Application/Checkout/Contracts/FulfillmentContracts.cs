namespace Application.Checkout.Contracts;

public interface IFulfillmentService
{
    Task RouteFulfillmentAsync(Guid orderId, CancellationToken ct = default);
}