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

    public async Task PublishOrderPaidAsync(Order order, DateTimeOffset statusAtUtc, CancellationToken ct = default)
    {
        // D-02: Idempotency key = OrderId + EventType + StatusAtUtc
        var idempotencyKey = $"{order.Id}:OrderPaid:{statusAtUtc:O}";

        // Check for duplicate
        if (await _outboxRepository.ExistsAsync(idempotencyKey, ct))
        {
            _logger.LogInformation("Skipping duplicate notification for order {OrderId} ({EventType})", order.Id, NotificationEventType.OrderPaid);
            return;
        }

        // D-09: Check if notification is available
        if (!order.NotificationAvailable || string.IsNullOrWhiteSpace(order.NotificationPhone))
        {
            _logger.LogWarning("Notification not available for order {OrderId}. Reason: {Reason}", order.Id, order.NotificationFailedReason ?? "missing-contact");

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
            NotificationType = NotificationType.PaymentApproved
        };

        _backgroundJobClient.Enqueue<OrderNotificationJob>(job => job.ExecuteAsync(args, ct));

        _logger.LogInformation("Enqueued OrderPaid notification for order {OrderId}, idempotencyKey: {Key}", order.Id, idempotencyKey);
    }

    public async Task PublishDeliveryCompletedAsync(Order order, DateTimeOffset statusAtUtc, CancellationToken ct = default)
    {
        var idempotencyKey = $"{order.Id}:DeliveryCompleted:{statusAtUtc:O}";

        if (await _outboxRepository.ExistsAsync(idempotencyKey, ct))
        {
            _logger.LogInformation("Skipping duplicate notification for order {OrderId} ({EventType})", order.Id, NotificationEventType.DeliveryCompleted);
            return;
        }

        if (!order.NotificationAvailable || string.IsNullOrWhiteSpace(order.NotificationPhone))
        {
            _logger.LogWarning("Notification not available for order {OrderId}.", order.Id);

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
            NotificationType = NotificationType.DeliveryCompleted
        };

        _backgroundJobClient.Enqueue<OrderNotificationJob>(job => job.ExecuteAsync(args, ct));

        _logger.LogInformation("Enqueued DeliveryCompleted notification for order {OrderId}", order.Id);
    }

    public async Task PublishDeliveryFailedAsync(Order order, string failureReason, DateTimeOffset statusAtUtc, CancellationToken ct = default)
    {
        var idempotencyKey = $"{order.Id}:DeliveryFailed:{statusAtUtc:O}";

        if (await _outboxRepository.ExistsAsync(idempotencyKey, ct))
        {
            _logger.LogInformation("Skipping duplicate notification for order {OrderId} ({EventType})", order.Id, NotificationEventType.DeliveryFailed);
            return;
        }

        if (!order.NotificationAvailable || string.IsNullOrWhiteSpace(order.NotificationPhone))
        {
            _logger.LogWarning("Notification not available for order {OrderId}.", order.Id);

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
            NotificationType = NotificationType.DeliveryStarted // Using DeliveryStarted as template for delivery issue
        };

        _backgroundJobClient.Enqueue<OrderNotificationJob>(job => job.ExecuteAsync(args, ct));

        _logger.LogInformation("Enqueued DeliveryFailed notification for order {OrderId}, reason: {Reason}", order.Id, failureReason);
    }
}