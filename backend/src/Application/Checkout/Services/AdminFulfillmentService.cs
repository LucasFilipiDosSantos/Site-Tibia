using Application.Checkout.Contracts;
using Domain.Checkout;

namespace Application.Checkout.Services;

public sealed class AdminFulfillmentService : IAdminFulfillmentService
{
    private readonly ICheckoutRepository _checkoutRepository;

    public AdminFulfillmentService(ICheckoutRepository checkoutRepository)
    {
        _checkoutRepository = checkoutRepository ?? throw new ArgumentNullException(nameof(checkoutRepository));
    }

    public async Task ForceCompleteAsync(Guid orderId, Guid productId, string adminNote, CancellationToken cancellationToken)
    {
        var order = await _checkoutRepository.GetOrderByIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order '{orderId}' not found.");

        var instruction = order.DeliveryInstructions.SingleOrDefault(x => x.ProductId == productId)
            ?? throw new InvalidOperationException($"Delivery instruction for product '{productId}' not found in order '{orderId}'.");

        if (instruction.Status != DeliveryStatus.Pending && instruction.Status != DeliveryStatus.Failed)
        {
            throw new InvalidOperationException($"Cannot force-complete delivery with status '{instruction.Status}'. Only Pending or Failed can be completed.");
        }

        instruction.Complete();
        
        await _checkoutRepository.SaveOrderAsync(order, cancellationToken);
    }
}