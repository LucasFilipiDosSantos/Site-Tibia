namespace Application.Payments.Contracts;

/// <summary>
/// Request payload for webhook signature validation
/// </summary>
public sealed record PaymentWebhookSignatureRequest(
    string DataId,
    string RequestId,
    string Timestamp,
    string Signature);

/// <summary>
/// Result of webhook signature validation
/// </summary>
public sealed class PaymentWebhookSignatureValidationResult
{
    public bool IsAccepted { get; private set; }
    public string? RejectionReason { get; private set; }

    private PaymentWebhookSignatureValidationResult() { }

    public static PaymentWebhookSignatureValidationResult Accepted() => new()
    {
        IsAccepted = true,
        RejectionReason = null
    };

    public static PaymentWebhookSignatureValidationResult Rejected(string reason) => new()
    {
        IsAccepted = false,
        RejectionReason = reason
    };
}

/// <summary>
/// Notification envelope from Mercado Pago webhook
/// </summary>
public sealed record PaymentWebhookNotification(
    string Type,
    string Action,
    string DataId,
    DateTimeOffset ReceivedAtUtc);

/// <summary>
/// Minimal inbound webhook log entry
/// </summary>
public sealed record PaymentWebhookLogEntry(
    Guid Id,
    string RequestId,
    string Topic,
    string Action,
    string ProviderResourceId,
    DateTimeOffset ReceivedAtUtc,
    PaymentWebhookValidationOutcome ValidationOutcome);

/// <summary>
/// Validation outcome for inbound webhook
/// </summary>
public enum PaymentWebhookValidationOutcome
{
    Accepted,
    RejectedMissingSignature,
    RejectedMalformedSignature,
    RejectedInvalidSignature,
    RejectedMissingHeaders
}

/// <summary>
/// Webhook processing outcome
/// </summary>
public sealed class WebhookProcessingOutcome
{
    public bool IsSuccess { get; private set; }
    public string? FailureReason { get; private set; }
    public bool IsDuplicate { get; private set; }
    public bool IsDeadLettered { get; private set; }

    private WebhookProcessingOutcome() { }

    public static WebhookProcessingOutcome Succeeded() => new() { IsSuccess = true };

    public static WebhookProcessingOutcome Duplicate() => new()
    {
        IsSuccess = true,
        IsDuplicate = true
    };

    public static WebhookProcessingOutcome Failed(string reason) => new()
    {
        IsSuccess = false,
        FailureReason = reason
    };

    public static WebhookProcessingOutcome DeadLettered(string reason) => new()
    {
        IsSuccess = false,
        IsDeadLettered = true,
        FailureReason = reason
    };
}