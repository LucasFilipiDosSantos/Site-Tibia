using Domain.Checkout;
using Application.Checkout.Contracts;
using Application.Inventory.Contracts;
using Application.Notifications;
using Microsoft.Extensions.Logging;

namespace Application.Checkout.Services;

public sealed class OrderLifecycleService
{
    private readonly IOrderLifecycleRepository _repository;
    private readonly IFulfillmentService _fulfillmentService;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly ILogger<OrderLifecycleService> _logger;
    private readonly ICheckoutInventoryGateway? _inventoryGateway;

    public OrderLifecycleService(
        IOrderLifecycleRepository repository,
        IFulfillmentService fulfillmentService,
        INotificationPublisher notificationPublisher,
        ILogger<OrderLifecycleService> logger,
        ICheckoutInventoryGateway? inventoryGateway = null)
    {
        _repository = repository;
        _fulfillmentService = fulfillmentService;
        _notificationPublisher = notificationPublisher;
        _logger = logger;
        _inventoryGateway = inventoryGateway;
    }

    /// <summary>
    /// D-01: Automatic notification enqueue triggers on Order Paid transition.
    /// D-03: Enqueue orchestration in Application lifecycle service.
    /// D-04: Failure does not rollback business transition.
    /// D-14: Correlation spans full chain: Payment -> Order -> Fulfillment -> Notification.
    /// </summary>
    public async Task ApplySystemTransitionAsync(Guid orderId, string? correlationId = null, CancellationToken cancellationToken = default)
    {
        var order = await _repository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order {orderId} not found.");

        var now = DateTimeOffset.UtcNow;
        order.ApplyTransition(OrderStatus.Paid, TransitionSourceType.System, now);
        await _repository.SaveAsync(order, cancellationToken);

        _logger.LogInformation(
            "Order {OrderId} transitioned to Paid with correlation ID {CorrelationId}",
            orderId,
            correlationId);

        // Route fulfillment after Paid transition (same transaction scope)
        await _fulfillmentService.RouteFulfillmentAsync(orderId, correlationId, cancellationToken);

        // D-01: Enqueue WhatsApp notification after successful Paid transition
        // D-04: Failure does not roll back the business transition
        try
        {
            await _notificationPublisher.PublishOrderPaidAsync(order, now, correlationId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to enqueue notification for order {OrderId} (CorrelationId: {CorrelationId}). Business transition completed.",
                orderId,
                correlationId);
        }
    }

    public async Task ApplyCustomerCancelAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _repository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order {orderId} not found.");

        order.ApplyTransition(OrderStatus.Cancelled, TransitionSourceType.Customer, DateTimeOffset.UtcNow);
        await _repository.SaveAsync(order, cancellationToken);
        await ReleaseReservationForCancelledOrderAsync(order, cancellationToken);
    }

    public async Task ApplyAdminCancelAsync(Guid orderId, Guid actorUserId, string reason, CancellationToken cancellationToken = default)
    {
        var order = await _repository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order {orderId} not found.");

        order.ApplyTransition(
            OrderStatus.Cancelled,
            TransitionSourceType.Admin,
            DateTimeOffset.UtcNow,
            actorUserId,
                string.IsNullOrWhiteSpace(reason) ? null : reason);

        await _repository.SaveAsync(order, cancellationToken);
        await ReleaseReservationForCancelledOrderAsync(order, cancellationToken);
    }

    public async Task ApplySystemCancelAsync(Guid orderId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var order = await _repository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order {orderId} not found.");

        if (order.Status == OrderStatus.Paid)
        {
            _logger.LogInformation("Order {OrderId} is already Paid; system cancellation ignored.", orderId);
            return;
        }

        order.ApplyTransition(
            OrderStatus.Cancelled,
            TransitionSourceType.System,
            DateTimeOffset.UtcNow,
            reason: string.IsNullOrWhiteSpace(reason) ? null : reason);

        await _repository.SaveAsync(order, cancellationToken);
        await ReleaseReservationForCancelledOrderAsync(order, cancellationToken);
    }

    private async Task ReleaseReservationForCancelledOrderAsync(Order order, CancellationToken cancellationToken)
    {
        if (_inventoryGateway is null)
        {
            return;
        }

        await _inventoryGateway.ReleaseCheckoutReservationAsync(
            order.OrderIntentKey,
            ReservationReleaseReason.OrderCanceled,
            cancellationToken);
    }
}
