using API.Auth;
using Application.Payments.Contracts;
using Application.Payments.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace API.Payments;

/// <summary>
/// Mercado Pago webhook ingress endpoint - validates signature and enqueues async processing (D-13)
/// </summary>
public static class PaymentWebhookEndpoints
{
    public static IEndpointRouteBuilder MapPaymentWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/payments/mercadopago")
            .AllowAnonymous(); // Auth handled via signature validation

        // POST /payments/mercadopago/webhook
        group.MapPost("/webhook", async (
            [FromHeader(Name = "x-signature")] string? signature,
            [FromHeader(Name = "x-request-id")] string? requestId,
            [FromBody] WebhookNotificationDto body,
            PaymentWebhookIngressService ingressService,
            IPaymentWebhookLogRepository logRepository,
            IBackgroundJobClient jobClient,
            CancellationToken ct) =>
        {
            // Parse notification envelope
            var notification = new PaymentWebhookNotification(
                Type: body.Type ?? "payment",
                Action: body.Action ?? "payment.created",
                DataId: body.Data?.Id ?? string.Empty,
                ReceivedAtUtc: DateTimeOffset.UtcNow
            );

            // Parse x-signature header: "ts=<timestamp>,v1=<signature>"
            var timestamp = "";
            var sigValue = "";
            if (!string.IsNullOrEmpty(signature))
            {
                var parts = signature.Split(',');
                foreach (var part in parts)
                {
                    var kv = part.Trim().Split('=');
                    if (kv.Length == 2)
                    {
                        if (kv[0] == "ts") timestamp = kv[1];
                        else if (kv[0] == "v1") sigValue = kv[1];
                    }
                }
            }

            // Build signature request
            var signatureRequest = new PaymentWebhookSignatureRequest(
                DataId: notification.DataId,
                RequestId: requestId ?? string.Empty,
                Timestamp: timestamp,
                Signature: sigValue
            );

            // D-04: Validate signature - fail-closed
            var validationResult = ingressService.ValidateSignature(signatureRequest);
            
            if (!validationResult.IsAccepted)
            {
                // Log rejection but don't mutate any order/payment state
                await logRepository.LogAsync(new PaymentWebhookLogEntry(
                    Id: Guid.NewGuid(),
                    RequestId: requestId ?? Guid.NewGuid().ToString(),
                    Topic: notification.Type,
                    Action: notification.Action,
                    ProviderResourceId: notification.DataId,
                    ReceivedAtUtc: notification.ReceivedAtUtc,
                    ValidationOutcome: PaymentWebhookValidationOutcome.RejectedInvalidSignature
                ), ct);

                return Results.BadRequest(new { error = validationResult.RejectionReason });
            }

            // D-13: Fast ack - persist minimal inbound log
            var logEntry = new PaymentWebhookLogEntry(
                Id: Guid.NewGuid(),
                RequestId: requestId ?? Guid.NewGuid().ToString(),
                Topic: notification.Type,
                Action: notification.Action,
                ProviderResourceId: notification.DataId,
                ReceivedAtUtc: notification.ReceivedAtUtc,
                ValidationOutcome: PaymentWebhookValidationOutcome.Accepted
            );
            
            await logRepository.LogAsync(logEntry, ct);

            // Enqueue async processing job
            jobClient.Enqueue<PaymentWebhookProcessor>(processor => 
                processor.ProcessAsync(logEntry.Id, ct));

            // Return 200 for idempotent retries, 201 for initial delivery
            return Results.Created($"/payments/webhook/{logEntry.Id}", new { requestId = logEntry.RequestId });
        });

        return app;
    }
}

/// <summary>
/// DTO for Mercado Pago webhook notification body
/// </summary>
public sealed class WebhookNotificationDto
{
    public string? Type { get; init; }
    public string? Action { get; init; }
    public WebhookDataDto? Data { get; init; }
}

/// <summary>
/// DTO for webhook data payload
/// </summary>
public sealed class WebhookDataDto
{
    public string? Id { get; init; }
}

/// <summary>
/// Background job client interface for Hangfire integration
/// </summary>
public interface IBackgroundJobClient
{
    void Enqueue<T>(Expression<Action<T>> methodCall);
}