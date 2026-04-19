using Application.Checkout.Contracts;
using Application.Notifications;
using Domain.Checkout;
using Microsoft.Extensions.Logging;

namespace Application.Checkout.Services;

public sealed class FulfillmentService : IFulfillmentService
{
    private readonly IOrderLifecycleRepository _repository;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly ILogger<FulfillmentService> _logger;

    public FulfillmentService(
        IOrderLifecycleRepository repository,
        INotificationPublisher notificationPublisher,
        ILogger<FulfillmentService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _notificationPublisher = notificationPublisher ?? throw new ArgumentNullException(nameof(notificationPublisher));
        _logger = logger;
    }

    /// <summary>
    /// Routes fulfillment for an order after Paid transition.
    /// Automated deliveries are immediately completed (D-01).
    /// </summary>
    public async Task RouteFulfillmentAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _repository.GetByIdAsync(orderId, ct)
            ?? throw new InvalidOperationException($"Order {orderId} not found.");

        var now = DateTimeOffset.UtcNow;

        foreach (var instruction in order.DeliveryInstructions)
        {
            if (instruction.FulfillmentType == FulfillmentType.Automated)
            {
                instruction.Complete();
                // D-01: Publish DeliveryCompleted notification
                try
                {
                    await _notificationPublisher.PublishDeliveryCompletedAsync(order, now, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to enqueue DeliveryCompleted notification for order {OrderId}.", orderId);
                }
            }
            // Manual fulfillment stays Pending for admin to complete later
        }

        await _repository.SaveAsync(order, ct);
    }

    /// <summary>
    /// Marks a delivery as failed and publishes notification (D-01).
    /// D-04: Failure does not rollback business transition.
    /// </summary>
    public async Task MarkDeliveryFailedAsync(Guid orderId, string failureReason, CancellationToken ct = default)
    {
        var order = await _repository.GetByIdAsync(orderId, ct)
            ?? throw new InvalidOperationException($"Order {orderId} not found.");

        var now = DateTimeOffset.UtcNow;

        foreach (var instruction in order.DeliveryInstructions)
        {
            if (instruction.Status == DeliveryStatus.Pending)
            {
                instruction.Fail(failureReason);
                // D-01: Publish DeliveryFailed notification
                try
                {
                    await _notificationPublisher.PublishDeliveryFailedAsync(order, failureReason, now, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to enqueue DeliveryFailed notification for order {OrderId}.", orderId);
                }
            }
        }

        await _repository.SaveAsync(order, ct);
    }
}