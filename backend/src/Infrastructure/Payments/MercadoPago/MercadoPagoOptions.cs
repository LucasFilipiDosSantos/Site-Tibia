namespace Infrastructure.Payments.MercadoPago;

public sealed class MercadoPagoOptions
{
    public const string SectionName = "MercadoPago";

    public string AccessToken { get; set; } = string.Empty;

    public string PublicKey { get; set; } = string.Empty;

    public string NotificationUrl { get; set; } = string.Empty;

    public string SuccessUrl { get; set; } = string.Empty;

    public string FailureUrl { get; set; } = string.Empty;

    public string PendingUrl { get; set; } = string.Empty;
}
