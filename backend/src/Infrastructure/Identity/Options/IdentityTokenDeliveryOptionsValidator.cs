using Microsoft.Extensions.Options;

namespace Infrastructure.Identity.Options;

public sealed class IdentityTokenDeliveryOptionsValidator
    : IValidateOptions<IdentityTokenDeliveryOptions>
{
    private static readonly HashSet<string> PlaceholderHosts =
    [
        "smtp.dev.local",
        "smtp.example.com",
    ];

    private static readonly HashSet<string> PlaceholderValues =
    [
        "change-me",
        "dev-user",
        "dev-password",
        "example",
    ];

    public ValidateOptionsResult Validate(string? name, IdentityTokenDeliveryOptions options)
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail("IdentityTokenDelivery options are required.");
        }

        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            return ValidateOptionsResult.Fail("IdentityTokenDelivery:Provider is required.");
        }

        if (!string.Equals(options.Provider, "smtp", StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Success;
        }

        var smtp = options.Smtp ?? new IdentityTokenDeliverySmtpOptions();

        if (string.IsNullOrWhiteSpace(smtp.Host))
        {
            return ValidateOptionsResult.Fail("IdentityTokenDelivery:Smtp:Host is required when Provider=smtp.");
        }

        if (PlaceholderHosts.Contains(smtp.Host.Trim()))
        {
            return ValidateOptionsResult.Fail(
                $"IdentityTokenDelivery:Smtp:Host '{smtp.Host}' is a placeholder and cannot be used at runtime. Set a reachable SMTP host via environment configuration."
            );
        }

        if (smtp.Port <= 0)
        {
            return ValidateOptionsResult.Fail("IdentityTokenDelivery:Smtp:Port must be greater than zero when Provider=smtp.");
        }

        if (string.IsNullOrWhiteSpace(smtp.Username))
        {
            return ValidateOptionsResult.Fail("IdentityTokenDelivery:Smtp:Username is required when Provider=smtp.");
        }

        if (string.IsNullOrWhiteSpace(smtp.Password))
        {
            return ValidateOptionsResult.Fail("IdentityTokenDelivery:Smtp:Password is required when Provider=smtp.");
        }

        if (IsPlaceholder(smtp.Username) || IsPlaceholder(smtp.Password))
        {
            return ValidateOptionsResult.Fail(
                "IdentityTokenDelivery SMTP credentials contain placeholder values and cannot be used at runtime. Set concrete credentials via environment configuration."
            );
        }

        if (string.IsNullOrWhiteSpace(smtp.FromEmail))
        {
            return ValidateOptionsResult.Fail("IdentityTokenDelivery:Smtp:FromEmail is required when Provider=smtp.");
        }

        return ValidateOptionsResult.Success;
    }

    private static bool IsPlaceholder(string value)
    {
        var normalized = value.Trim();
        return PlaceholderValues.Contains(normalized);
    }
}
