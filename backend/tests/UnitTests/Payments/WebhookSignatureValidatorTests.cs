using System.Security.Cryptography;
using System.Text;
using Application.Payments.Contracts;
using Infrastructure.Payments.MercadoPago;
using Microsoft.Extensions.Options;

namespace UnitTests.Payments;

public class WebhookSignatureValidatorTests
{
    [Fact]
    public void ValidateSignature_WithValidSignature_ReturnsAccepted()
    {
        // Arrange
        var secret = "test-webhook-secret";
        var dataId = "payment-12345";
        var requestId = "req-abcde";
        var ts = "1234567890";
        
        // Create valid signature: HMAC-SHA256("id:payment-12345;request-id:req-abcde;ts:1234567890;", secret)
        var manifest = $"id:{dataId};request-id:{requestId};ts:{ts};";
        var expectedSignature = ComputeHmacSha256(manifest, secret);
        
        var options = Options.Create(new MercadoPagoOptions { WebhookSecret = secret });
        var validator = new MercadoPagoWebhookSignatureValidator(options);
        var request = new PaymentWebhookSignatureRequest(
            DataId: dataId,
            RequestId: requestId,
            Timestamp: ts,
            Signature: expectedSignature
        );

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsAccepted);
        Assert.Null(result.RejectionReason);
    }

    [Fact]
    public void ValidateSignature_WithUppercaseDataId_LowercasedBeforeDigest()
    {
        // Arrange - Mercado Pago sends data.id UPPERCASE in notification
        var secret = "test-webhook-secret";
        var dataId = "PAYMENT-12345"; // Uppercase from Mercado Pago
        var requestId = "req-abcde";
        var ts = "1234567890";
        
        // Valid signature uses lowercase data.id in manifest
        var manifest = $"id:{dataId.ToLowerInvariant()};request-id:{requestId};ts:{ts};";
        var expectedSignature = ComputeHmacSha256(manifest, secret);
        
        var options = Options.Create(new MercadoPagoOptions { WebhookSecret = secret });
        var validator = new MercadoPagoWebhookSignatureValidator(options);
        var request = new PaymentWebhookSignatureRequest(
            DataId: dataId,
            RequestId: requestId,
            Timestamp: ts,
            Signature: expectedSignature
        );

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsAccepted);
    }

    [Fact]
    public void ValidateSignature_WithMissingSignature_ReturnsRejected()
    {
        // Arrange
        var secret = "test-webhook-secret";
        var options = Options.Create(new MercadoPagoOptions { WebhookSecret = secret });
        var validator = new MercadoPagoWebhookSignatureValidator(options);
        var request = new PaymentWebhookSignatureRequest(
            DataId: "payment-12345",
            RequestId: "req-abcde",
            Timestamp: "1234567890",
            Signature: null!
        );

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsAccepted);
        Assert.NotNull(result.RejectionReason);
        Assert.Contains("missing", result.RejectionReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateSignature_WithMalformedSignature_ReturnsRejected()
    {
        // Arrange
        var secret = "test-webhook-secret";
        var options = Options.Create(new MercadoPagoOptions { WebhookSecret = secret });
        var validator = new MercadoPagoWebhookSignatureValidator(options);
        var request = new PaymentWebhookSignatureRequest(
            DataId: "payment-12345",
            RequestId: "req-abcde",
            Timestamp: "1234567890",
            Signature: "invalid-signature-format"
        );

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsAccepted);
        Assert.NotNull(result.RejectionReason);
    }

    [Fact]
    public void ValidateSignature_WithMismatchedSignature_ReturnsRejected()
    {
        // Arrange
        var secret = "test-webhook-secret";
        var options = Options.Create(new MercadoPagoOptions { WebhookSecret = secret });
        var validator = new MercadoPagoWebhookSignatureValidator(options);
        
        // Use valid prefix but wrong signature content
        var request = new PaymentWebhookSignatureRequest(
            DataId: "payment-12345",
            RequestId: "req-abcde",
            Timestamp: "1234567890",
            Signature: "v1:0000000000000000000000000000000000000000"  // Valid format, wrong content
        );

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsAccepted);
        Assert.NotNull(result.RejectionReason);
        Assert.Contains("mismatch", result.RejectionReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateSignature_WithMissingTimestamp_ReturnsRejected()
    {
        // Arrange
        var secret = "test-webhook-secret";
        var options = Options.Create(new MercadoPagoOptions { WebhookSecret = secret });
        var validator = new MercadoPagoWebhookSignatureValidator(options);
        var request = new PaymentWebhookSignatureRequest(
            DataId: "payment-12345",
            RequestId: "req-abcde",
            Timestamp: null!,
            Signature: "v1_abc123"
        );

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsAccepted);
        Assert.NotNull(result.RejectionReason);
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return "v1:" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}