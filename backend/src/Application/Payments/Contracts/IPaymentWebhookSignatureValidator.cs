using Application.Payments.Contracts;

namespace Application.Payments.Contracts;

/// <summary>
/// Validates Mercado Pago webhook x-signature header
/// </summary>
public interface IPaymentWebhookSignatureValidator
{
    /// <summary>
    /// Validates the webhook signature using Mercado Pago canonical manifest format.
    /// </summary>
    PaymentWebhookSignatureValidationResult Validate(PaymentWebhookSignatureRequest request);
}