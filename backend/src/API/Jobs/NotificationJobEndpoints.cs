using API.Auth;
using Application.Notifications;
using Hangfire;
using Infrastructure.Notifications;
using Microsoft.AspNetCore.Mvc;

namespace API.Jobs;

public static class NotificationJobEndpoints
{
    public static void MapNotificationJobEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/jobs/notifications").RequireAuthorization(AuthPolicies.AdminOnly);

        group.MapPost("/trigger", TriggerNotification);
        group.MapPost("/retry/{orderId}", RetryNotification);
    }

    private static async Task<IResult> TriggerNotification(
        [FromBody] TriggerNotificationRequest request,
        IBackgroundJobClient backgroundJobClient,
        CancellationToken ct)
    {
        if (request.OrderId == Guid.Empty || string.IsNullOrWhiteSpace(request.CustomerPhone))
        {
            return Results.BadRequest("OrderId and CustomerPhone are required.");
        }

        var args = new OrderNotificationJobArgs
        {
            OrderId = request.OrderId,
            OrderNumber = request.OrderNumber ?? request.OrderId.ToString()[..8],
            CustomerPhone = request.CustomerPhone,
            NotificationType = request.NotificationType
        };

        var jobId = backgroundJobClient.Enqueue<OrderNotificationJob>(job =>
            job.ExecuteAsync(args, ct));

        return Results.Ok(new { jobId, orderId = request.OrderId });
    }

    private static async Task<IResult> RetryNotification(
        [FromRoute] Guid orderId,
        [FromQuery] string phone,
        [FromQuery] string orderNumber,
        IBackgroundJobClient backgroundJobClient,
        CancellationToken ct)
    {
        if (orderId == Guid.Empty || string.IsNullOrWhiteSpace(phone))
        {
            return Results.BadRequest("orderId and phone are required.");
        }

        var args = new OrderNotificationJobArgs
        {
            OrderId = orderId,
            OrderNumber = orderNumber ?? orderId.ToString()[..8],
            CustomerPhone = phone,
            NotificationType = NotificationType.PaymentApproved
        };

        var jobId = backgroundJobClient.Enqueue<OrderNotificationJob>(job =>
            job.ExecuteAsync(args, ct));

        return Results.Ok(new { jobId, orderId });
    }
}

public class TriggerNotificationRequest
{
    public Guid OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public string CustomerPhone { get; set; } = string.Empty;
    public NotificationType NotificationType { get; set; }
}
