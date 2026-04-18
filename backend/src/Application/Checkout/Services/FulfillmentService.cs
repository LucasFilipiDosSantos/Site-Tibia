using Application.Checkout.Contracts;
using Domain.Checkout;

namespace Application.Checkout.Services;

public sealed class FulfillmentService : IFulfillmentService
{
    private readonly IOrderLifecycleRepository _repository;

    public FulfillmentService(IOrderLifecycleRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task RouteFulfillmentAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _repository.GetByIdAsync(orderId, ct)
            ?? throw new InvalidOperationException($"Order {orderId} not found.");

        foreach (var instruction in order.DeliveryInstructions)
        {
            if (instruction.FulfillmentType == FulfillmentType.Automated)
            {
                instruction.Complete();
            }
            // Manual fulfillment stays Pending for admin to complete later
        }

        await _repository.SaveAsync(order, ct);
    }
}