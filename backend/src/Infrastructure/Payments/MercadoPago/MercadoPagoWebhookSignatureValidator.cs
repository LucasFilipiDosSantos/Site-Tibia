using System.Security.Cryptography;
using System.Text;
using Application.Payments.Contracts;
using Microsoft.Extensions.Options;
using Infrastructure.Payments.MercadoPago;

namespace Infrastructure.Payments.MercadoPago;

/// <summary>
/// Validates Mercado Pago x-signature webhook header using canonical manifest format (D-04, D-05).
/// Manifest format: id:{data.id_lowercase};request-id:{x-request-id};ts:{ts};
/// </summary>
public sealed class MercadoPagoWebhookSignatureValidator : IPaymentWebhookSignatureValidator
{
    private readonly string _webhookSecret;

    public MercadoPagoWebhookSignatureValidator(IOptions<MercadoPagoOptions> options)
    {
        var opts = options.Value;
        _webhookSecret = opts.WebhookSecret 
            ?? throw new InvalidOperationException("MercadoPago:WebhookSecret configuration is required.");
    }

    /// <inheritdoc/>
    public PaymentWebhookSignatureValidationResult Validate(PaymentWebhookSignatureRequest request)
    {
        // D-04: Fail-closed - reject if missing signature
        if (string.IsNullOrWhiteSpace(request.Signature))
        {
            return PaymentWebhookSignatureValidationResult.Rejected("Missing x-signature header");
        }

        // D-04: Fail-closed - reject if missing timestamp
        if (string.IsNullOrWhiteSpace(request.Timestamp))
        {
            return PaymentWebhookSignatureValidationResult.Rejected("Missing timestamp in signature");
        }

        // D-04: Fail-closed - reject if missing data id
        if (string.IsNullOrWhiteSpace(request.DataId))
        {
            return PaymentWebhookSignatureValidationResult.Rejected("Missing data.id in payload");
        }

        // Validate signature format: must start with "v1:"
        if (!request.Signature.StartsWith("v1:", StringComparison.OrdinalIgnoreCase))
        {
            return PaymentWebhookSignatureValidationResult.Rejected(
                "Malformed signature - must start with 'v1:'");
        }

        // D-05: Canonical manifest format - MUST lowercase data.id
        // Format: id:{data.id_lowercase};request-id:{x-request-id};ts:{ts};
        var manifest = new StringBuilder()
            .Append("id:")
            .Append(request.DataId.ToLowerInvariant())
            .Append(";request-id:")
            .Append(request.RequestId ?? string.Empty)
            .Append(";ts:")
            .Append(request.Timestamp)
            .Append(';')
            .ToString();

        // Compute HMAC-SHA256 hex
        var expectedSignature = ComputeHmacSha256(manifest, _webhookSecret);

        // D-05: Constant-time comparison to prevent timing attacks
        if (!ConstantTimeEquals(request.Signature, expectedSignature))
        {
            return PaymentWebhookSignatureValidationResult.Rejected("Signature mismatch");
        }

        return PaymentWebhookSignatureValidationResult.Accepted();
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return "v1:" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool ConstantTimeEquals(string a, string b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a),
            Encoding.UTF8.GetBytes(b));
    }
}