using Infrastructure.Payments.MercadoPago;
using Microsoft.Extensions.Options;

namespace UnitTests.Payments;

public sealed class MercadoPagoOptionsValidatorTests
{
    private readonly MercadoPagoOptionsValidator _validator = new();

    [Fact]
    public void Validate_WhenDisabled_AllowsEmptyConfiguration()
    {
        var options = new MercadoPagoOptions
        {
            Enabled = false
        };

        var result = _validator.Validate(Options.DefaultName, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_WhenEnabled_RequiresAbsoluteUrlsAndCredentials()
    {
        var options = new MercadoPagoOptions
        {
            Enabled = true,
            AccessToken = "token",
            PublicKey = "public-key",
            NotificationUrl = "https://example.com/webhook",
            SuccessUrl = "https://example.com/success",
            FailureUrl = "https://example.com/failure",
            PendingUrl = "https://example.com/pending"
        };

        var result = _validator.Validate(Options.DefaultName, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_WhenEnabled_RejectsMissingFailureUrl()
    {
        var options = new MercadoPagoOptions
        {
            Enabled = true,
            AccessToken = "token",
            PublicKey = "public-key",
            NotificationUrl = "https://example.com/webhook",
            SuccessUrl = "https://example.com/success",
            FailureUrl = string.Empty,
            PendingUrl = "https://example.com/pending"
        };

        var result = _validator.Validate(Options.DefaultName, options);

        Assert.False(result.Succeeded);
        Assert.Contains("MercadoPago:FailureUrl", result.FailureMessage);
    }
}
