using Application.Notifications;
using Domain.Checkout;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Notifications;

/// <summary>
/// Implementation of INotificationPublisher that enqueues Hangfire jobs
/// with idempotency checking via NotificationOutbox.
/// D-01: Automatic notification enqueue on lifecycle events.
/// D-02: Idempotency key = OrderId + EventType + StatusAtUtc
/// D-04: Failure does not rollback business transition.
/// D-14: Correlation spans full chain: Payment -> Order -> Fulfillment -> Notification.
/// </summary>
public sealed class NotificationPublisher : INotificationPublisher
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly INotificationOutboxRepository _outboxRepository;
    private readonly ILogger<NotificationPublisher> _logger;

    public NotificationPublisher(
        IBackgroundJobClient backgroundJobClient,
        INotificationOutboxRepository outboxRepository,
        ILogger<NotificationPublisher> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _outboxRepository = outboxRepository;
        _logger = logger;
    }

    public async Task PublishOrderPaidAsync(Order order, DateTimeOffset statusAtUtc, string? correlationId = null, CancellationToken ct = default)
    {
        // D-02: Idempotency key = OrderId + EventType + StatusAtUtc
        var idempotencyKey = $"{order.Id}:OrderPaid:{statusAtUtc:O}";

        // Check for duplicate
        if (await _outboxRepository.ExistsAsync(idempotencyKey, ct))
        {
            _logger.LogInformation(
                "Skipping duplicate notification for order {OrderId} ({EventType}), correlation ID: {CorrelationId}",
                order.Id,
                NotificationEventType.OrderPaid,
                correlationId);
            return;
        }

        // D-09: Check if notification is available
        if (!order.NotificationAvailable || string.IsNullOrWhiteSpace(order.NotificationPhone))
        {
            _logger.LogWarning(
                "Notification not available for order {OrderId}. Reason: {Reason}, correlation ID: {CorrelationId}",
                order.Id,
                order.NotificationFailedReason ?? "missing-contact",
                correlationId);

            // Save to outbox for potential retry later (D-04)
            await _outboxRepository.SaveFailedAsync(new NotificationOutboxEntry
            {
                IdempotencyKey = idempotencyKey,
                OrderId = order.Id,
                EventType = NotificationEventType.OrderPaid,
                StatusAtUtc = statusAtUtc,
                NotificationPhone = null,
                FailureReason = order.NotificationFailedReason ?? "missing-contact",
                RetryCount = 0
            }, ct);

            return;
        }

        // Enqueue notification job
        var args = new OrderNotificationJobArgs
        {
            OrderId = order.Id,
            OrderNumber = order.OrderIntentKey,
            CustomerPhone = order.NotificationPhone,
            NotificationType = NotificationType.PaymentApproved,
            CorrelationId = correlationId
        };

        _backgroundJobClient.Enqueue<OrderNotificationJob>(job => job.ExecuteAsync(args, ct));

        _logger.LogInformation(
            "Enqueued OrderPaid notification for order {OrderId}, idempotencyKey: {Key}, correlation ID: {CorrelationId}",
            order.Id,
            idempotencyKey,
            correlationId);
    }

    public async Task PublishDeliveryCompletedAsync(Order order, DateTimeOffset statusAtUtc, string? correlationId = null, CancellationToken ct = default)
    {
        var idempotencyKey = $"{order.Id}:DeliveryCompleted:{statusAtUtc:O}";

        if (await _outboxRepository.ExistsAsync(idempotencyKey, ct))
        {
            _logger.LogInformation(
                "Skipping duplicate notification for order {OrderId} ({EventType}), correlation ID: {CorrelationId}",
                order.Id,
                NotificationEventType.DeliveryCompleted,
                correlationId);
            return;
        }

        if (!order.NotificationAvailable || string.IsNullOrWhiteSpace(order.NotificationPhone))
        {
            _logger.LogWarning(
                "Notification not available for order {OrderId}, correlation ID: {CorrelationId}.",
                order.Id,
                correlationId);

            await _outboxRepository.SaveFailedAsync(new NotificationOutboxEntry
            {
                IdempotencyKey = idempotencyKey,
                OrderId = order.Id,
                EventType = NotificationEventType.DeliveryCompleted,
                StatusAtUtc = statusAtUtc,
                NotificationPhone = null,
                FailureReason = order.NotificationFailedReason ?? "missing-contact",
                RetryCount = 0
            }, ct);

            return;
        }

        var args = new OrderNotificationJobArgs
        {
            OrderId = order.Id,
            OrderNumber = order.OrderIntentKey,
            CustomerPhone = order.NotificationPhone,
            NotificationType = NotificationType.DeliveryCompleted,
            CorrelationId = correlationId
        };

        _backgroundJobClient.Enqueue<OrderNotificationJob>(job => job.ExecuteAsync(args, ct));

        _logger.LogInformation(
            "Enqueued DeliveryCompleted notification for order {OrderId}, correlation ID: {CorrelationId}",
            order.Id,
            correlationId);
    }

    public async Task PublishDeliveryFailedAsync(Order order, string failureReason, DateTimeOffset statusAtUtc, string? correlationId = null, CancellationToken ct = default)
    {
        var idempotencyKey = $"{order.Id}:DeliveryFailed:{statusAtUtc:O}";

        if (await _outboxRepository.ExistsAsync(idempotencyKey, ct))
        {
            _logger.LogInformation(
                "Skipping duplicate notification for order {OrderId} ({EventType}), correlation ID: {CorrelationId}",
                order.Id,
                NotificationEventType.DeliveryFailed,
                correlationId);
            return;
        }

        if (!order.NotificationAvailable || string.IsNullOrWhiteSpace(order.NotificationPhone))
        {
            _logger.LogWarning(
                "Notification not available for order {OrderId}, correlation ID: {CorrelationId}.",
                order.Id,
                correlationId);

            await _outboxRepository.SaveFailedAsync(new NotificationOutboxEntry
            {
                IdempotencyKey = idempotencyKey,
                OrderId = order.Id,
                EventType = NotificationEventType.DeliveryFailed,
                StatusAtUtc = statusAtUtc,
                NotificationPhone = null,
                FailureReason = order.NotificationFailedReason ?? "missing-contact",
                RetryCount = 0
            }, ct);

            return;
        }

        var args = new OrderNotificationJobArgs
        {
            OrderId = order.Id,
            OrderNumber = order.OrderIntentKey,
            CustomerPhone = order.NotificationPhone,
            NotificationType = NotificationType.DeliveryStarted,
            CorrelationId = correlationId
        };

        _backgroundJobClient.Enqueue<OrderNotificationJob>(job => job.ExecuteAsync(args, ct));

        _logger.LogInformation(
            "Enqueued DeliveryFailed notification for order {OrderId}, reason: {Reason}, correlation ID: {CorrelationId}",
            order.Id,
            failureReason,
            correlationId);
    }
}