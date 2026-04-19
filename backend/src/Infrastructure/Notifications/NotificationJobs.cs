using Application.Notifications;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Notifications;

/// <summary>
/// D-14: Correlation spans full chain - includes correlation ID for observability.
/// </summary>
public class OrderNotificationJobArgs
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public NotificationType NotificationType { get; set; }
    public string? CorrelationId { get; set; }
}

[AutomaticRetry(Attempts = 5, DelaysInSeconds = new[] { 60, 300, 900, 3600, 86400 })]
public class OrderNotificationJob
{
    private readonly IWhatsAppNotificationService _whatsAppService;
    private readonly ILogger<OrderNotificationJob> _logger;

    public OrderNotificationJob(
        IWhatsAppNotificationService whatsAppService,
        ILogger<OrderNotificationJob> logger)
    {
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task ExecuteAsync(OrderNotificationJobArgs args, CancellationToken ct = default)
    {
        var templateName = args.NotificationType switch
        {
            NotificationType.OrderConfirmation => "order_created",
            NotificationType.PaymentApproved => "payment_approved",
            NotificationType.DeliveryStarted => "delivery_started",
            NotificationType.DeliveryCompleted => "delivery_completed",
            _ => "order_created"
        };

        var languageCode = "en_US";

        var parameters = new Dictionary<string, string>
        {
            ["order_number"] = args.OrderNumber
        };

        var messageId = await _whatsAppService.SendTemplateMessageAsync(
            args.CustomerPhone,
            templateName,
            languageCode,
            parameters,
            ct);

        // D-14: Log with correlation ID for full chain observability
        _logger.LogInformation(
            "Notification sent for order {OrderId} ({OrderNumber}), type: {NotificationType}, messageId: {MessageId}, correlationId: {CorrelationId}",
            args.OrderId, args.OrderNumber, args.NotificationType, messageId, args.CorrelationId);
    }
}

public class PaymentNotificationJobArgs
{
    public Guid OrderId { get; set; }
    public Guid PaymentId { get; set; }
    public string CustomerPhone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

[AutomaticRetry(Attempts = 5, DelaysInSeconds = new[] { 60, 300, 900, 3600, 86400 })]
public class PaymentNotificationJob
{
    private readonly IWhatsAppNotificationService _whatsAppService;
    private readonly ILogger<PaymentNotificationJob> _logger;

    public PaymentNotificationJob(
        IWhatsAppNotificationService whatsAppService,
        ILogger<PaymentNotificationJob> logger)
    {
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task ExecuteAsync(PaymentNotificationJobArgs args, CancellationToken ct = default)
    {
        var templateName = "payment_approved";
        var languageCode = "en_US";

        var parameters = new Dictionary<string, string>
        {
            ["payment_status"] = args.Status
        };

        var messageId = await _whatsAppService.SendTemplateMessageAsync(
            args.CustomerPhone,
            templateName,
            languageCode,
            parameters,
            ct);

        _logger.LogInformation(
            "Payment notification sent for payment {PaymentId}, status: {Status}, messageId: {MessageId}",
            args.PaymentId, args.Status, messageId);
    }
}