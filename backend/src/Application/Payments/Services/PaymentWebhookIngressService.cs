using Application.Payments.Contracts;

namespace Application.Payments.Services;

/// <summary>
/// Webhook trust gate - validates signature before any mutation (D-04, D-05)
/// </summary>
public sealed class PaymentWebhookIngressService
{
    private readonly IPaymentWebhookSignatureValidator _signatureValidator;

    public PaymentWebhookIngressService(IPaymentWebhookSignatureValidator signatureValidator)
    {
        _signatureValidator = signatureValidator ?? throw new ArgumentNullException(nameof(signatureValidator));
    }

    /// <summary>
    /// Validates webhook authenticity. Returns reject outcome if invalid - never emits mutation command.
    /// </summary>
    public PaymentWebhookSignatureValidationResult ValidateSignature(PaymentWebhookSignatureRequest request)
    {
        return _signatureValidator.Validate(request);
    }

    /// <summary>
    /// Checks if validation result indicates acceptance.
    /// </summary>
    public static bool IsAccepted(PaymentWebhookSignatureValidationResult result) => result.IsAccepted;
}

/// <summary>
/// Result from webhook ingress processing
/// </summary>
public sealed class PaymentWebhookIngressResult
{
    public bool IsAccepted { get; private set; }
    public Guid? RequestId { get; private set; }
    public string? RejectionReason { get; private set; }
    public PaymentWebhookValidationOutcome ValidationOutcome { get; private set; }

    private PaymentWebhookIngressResult() { }

    public static PaymentWebhookIngressResult Accepted(Guid requestId, PaymentWebhookValidationOutcome outcome) => new()
    {
        IsAccepted = true,
        RequestId = requestId,
        ValidationOutcome = outcome
    };

    public static PaymentWebhookIngressResult Rejected(string reason) => new()
    {
        IsAccepted = false,
        RejectionReason = reason,
        ValidationOutcome = PaymentWebhookValidationOutcome.RejectedMissingSignature
    };
}