namespace Application.Notifications;

public enum NotificationType
{
    OrderConfirmation,
    PaymentApproved,
    DeliveryStarted,
    DeliveryCompleted
}

public record NotificationPayload(
    NotificationType Type,
    string RecipientPhone,
    string TemplateName,
    Dictionary<string, string> Parameters
);