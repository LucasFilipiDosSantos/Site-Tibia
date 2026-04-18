namespace Application.Notifications;

public interface INotificationService
{
    Task SendAsync(NotificationPayload payload, CancellationToken ct = default);
    Task<bool> TrySendAsync(NotificationPayload payload, CancellationToken ct = default);
}