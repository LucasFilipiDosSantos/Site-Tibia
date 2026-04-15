namespace Infrastructure.Identity.Options;

public sealed class IdentityTokenDeliveryOptions
{
    public const string SectionName = "IdentityTokenDelivery";

    public string Provider { get; set; } = "smtp";

    public IdentityTokenDeliverySmtpOptions Smtp { get; set; } = new();
}

public sealed class IdentityTokenDeliverySmtpOptions
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromEmail { get; set; } = string.Empty;

    public bool UseTls { get; set; } = true;
}
