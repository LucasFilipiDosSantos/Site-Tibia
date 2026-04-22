using API.Auth;
using Application.Payments.Contracts;
using Application.Payments.Services;
using Hangfire;
using Infrastructure.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Payments;

/// <summary>
/// Mercado Pago webhook ingress endpoint - validates signature and enqueues async processing (D-13)
/// D-14: Correlation spans full chain - extract or generate correlation ID
/// D-17: Observability closure includes failure-path assertions
/// </summary>
public static class PaymentWebhookEndpoints
{
    public static IEndpointRouteBuilder MapPaymentWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/payments/mercadopago")
            .AllowAnonymous();

        group.MapPost("/webhook", async (
            [FromHeader(Name = "x-signature")] string? signature,
            [FromHeader(Name = "x-request-id")] string? requestId,
            [FromHeader(Name = RequestLoggingMiddleware.CorrelationIdHeader)] string? correlationIdHeader,
            [FromBody] WebhookNotificationDto body,
            [FromServices] PaymentWebhookIngressService ingressService,
            [FromServices] IPaymentWebhookLogRepository logRepository,
            [FromServices] Hangfire.IBackgroundJobClient jobClient,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var correlationId = correlationIdHeader
                ?? httpContext.Items[RequestLoggingMiddleware.CorrelationIdItemKey] as string
                ?? Guid.NewGuid().ToString("N");

            var notification = new PaymentWebhookNotification(
                Type: body.Type ?? "payment",
                Action: body.Action ?? "payment.created",
                DataId: body.Data?.Id ?? string.Empty,
                ReceivedAtUtc: DateTimeOffset.UtcNow
            );

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
                        if (kv[0] == "ts")
                        {
                            timestamp = kv[1];
                        }
                        else if (kv[0] == "v1")
                        {
                            sigValue = kv[1];
                        }
                    }
                }
            }

            var signatureRequest = new PaymentWebhookSignatureRequest(
                DataId: notification.DataId,
                RequestId: requestId ?? string.Empty,
                Timestamp: timestamp,
                Signature: sigValue
            );

            var validationResult = ingressService.ValidateSignature(signatureRequest);

            if (!validationResult.IsAccepted)
            {
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

            jobClient.Enqueue<PaymentWebhookProcessor>(processor =>
                processor.ProcessAsync(logEntry.Id, correlationId, ct));

            return Results.Created($"/payments/webhook/{logEntry.Id}", new { requestId = logEntry.RequestId, correlationId });
        });

        return app;
    }
}

public sealed class WebhookNotificationDto
{
    public string? Type { get; init; }
    public string? Action { get; init; }
    public WebhookDataDto? Data { get; init; }
}

public sealed class WebhookDataDto
{
    public string? Id { get; init; }
}
