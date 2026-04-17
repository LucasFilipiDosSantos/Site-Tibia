using Microsoft.Extensions.Options;

namespace Infrastructure.Payments.MercadoPago;

public sealed class MercadoPagoOptionsValidator : IValidateOptions<MercadoPagoOptions>
{
    public ValidateOptionsResult Validate(string? name, MercadoPagoOptions options)
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail("MercadoPago options are required.");
        }

        if (string.IsNullOrWhiteSpace(options.AccessToken))
        {
            return ValidateOptionsResult.Fail("MercadoPago:AccessToken is required.");
        }

        if (string.IsNullOrWhiteSpace(options.PublicKey))
        {
            return ValidateOptionsResult.Fail("MercadoPago:PublicKey is required.");
        }

        if (!IsAbsoluteHttpUrl(options.NotificationUrl))
        {
            return ValidateOptionsResult.Fail("MercadoPago:NotificationUrl must be an absolute HTTP/HTTPS URL.");
        }

        if (!IsAbsoluteHttpUrl(options.SuccessUrl))
        {
            return ValidateOptionsResult.Fail("MercadoPago:SuccessUrl must be an absolute HTTP/HTTPS URL.");
        }

        if (!IsAbsoluteHttpUrl(options.FailureUrl))
        {
            return ValidateOptionsResult.Fail("MercadoPago:FailureUrl must be an absolute HTTP/HTTPS URL.");
        }

        if (!IsAbsoluteHttpUrl(options.PendingUrl))
        {
            return ValidateOptionsResult.Fail("MercadoPago:PendingUrl must be an absolute HTTP/HTTPS URL.");
        }

        return ValidateOptionsResult.Success;
    }

    private static bool IsAbsoluteHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
