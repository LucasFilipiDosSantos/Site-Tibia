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
    /// D-14: Routes fulfillment for an order after Paid transition with correlation ID.
    /// Automated deliveries are immediately completed (D-01).
    /// </summary>
    public async Task RouteFulfillmentAsync(Guid orderId, string? correlationId = null, CancellationToken ct = default)
    {
        var order = await _repository.GetByIdAsync(orderId, ct)
            ?? throw new InvalidOperationException($"Order {orderId} not found.");

        var now = DateTimeOffset.UtcNow;

        foreach (var instruction in order.DeliveryInstructions)
        {
            if (instruction.FulfillmentType == FulfillmentType.Automated)
            {
                instruction.Complete();
                // D-01: Publish DeliveryCompleted notification with correlation ID
                try
                {
                    await _notificationPublisher.PublishDeliveryCompletedAsync(order, now, correlationId, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to enqueue DeliveryCompleted notification for order {OrderId} (CorrelationId: {CorrelationId}).",
                        orderId,
                        correlationId);
                }
            }
            // Manual fulfillment stays Pending for admin to complete later
        }

        _logger.LogInformation(
            "Fulfillment routed for order {OrderId} with correlation ID {CorrelationId}",
            orderId,
            correlationId);

        await _repository.SaveAsync(order, ct);
    }

    /// <summary>
    /// D-14: Marks a delivery as failed and publishes notification (D-01).
    /// D-04: Failure does not rollback business transition.
    /// </summary>
    public async Task MarkDeliveryFailedAsync(Guid orderId, string failureReason, string? correlationId = null, CancellationToken ct = default)
    {
        var order = await _repository.GetByIdAsync(orderId, ct)
            ?? throw new InvalidOperationException($"Order {orderId} not found.");

        var now = DateTimeOffset.UtcNow;

        foreach (var instruction in order.DeliveryInstructions)
        {
            if (instruction.Status == DeliveryStatus.Pending)
            {
                instruction.Fail(failureReason);
                // D-01: Publish DeliveryFailed notification with correlation ID
                try
                {
                    await _notificationPublisher.PublishDeliveryFailedAsync(order, failureReason, now, correlationId, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to enqueue DeliveryFailed notification for order {OrderId} (CorrelationId: {CorrelationId}).",
                        orderId,
                        correlationId);
                }
            }
        }

        _logger.LogInformation(
            "Fulfillment marked as failed for order {OrderId}, reason: {FailureReason}, correlation ID: {CorrelationId}",
            orderId,
            failureReason,
            correlationId);

        await _repository.SaveAsync(order, ct);
    }
}