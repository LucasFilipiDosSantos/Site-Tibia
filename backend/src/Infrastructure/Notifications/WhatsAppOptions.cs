using Microsoft.Extensions.Options;

namespace Infrastructure.Notifications;

public sealed class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    public string AccessToken { get; set; } = string.Empty;
    public string PhoneNumberId { get; set; } = string.Empty;
    public string WhatsAppBusinessId { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "v25.0";
    public string BaseUrl { get; set; } = "https://graph.facebook.com";
}

public sealed class WhatsAppOptionsValidator : IValidateOptions<WhatsAppOptions>
{
    public ValidateOptionsResult Validate(string? name, WhatsAppOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.AccessToken))
        {
            return ValidateOptionsResult.Fail($"{nameof(WhatsAppOptions.AccessToken)} is required.");
        }

        if (string.IsNullOrWhiteSpace(options.PhoneNumberId))
        {
            return ValidateOptionsResult.Fail($"{nameof(WhatsAppOptions.PhoneNumberId)} is required.");
        }

        if (string.IsNullOrWhiteSpace(options.WhatsAppBusinessId))
        {
            return ValidateOptionsResult.Fail($"{nameof(WhatsAppOptions.WhatsAppBusinessId)} is required.");
        }

        return ValidateOptionsResult.Success;
    }
}