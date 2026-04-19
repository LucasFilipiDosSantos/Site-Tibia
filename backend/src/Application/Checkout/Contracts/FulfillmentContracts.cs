namespace Application.Checkout.Contracts;

/// <summary>
/// D-14: Correlation spans full chain.
/// </summary>
public interface IFulfillmentService
{
    Task RouteFulfillmentAsync(Guid orderId, string? correlationId = null, CancellationToken ct = default);
}