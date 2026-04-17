using Application.Payments.Contracts;
using MercadoPago.Client.Preference;
using MercadoPago.Config;

namespace Infrastructure.Payments.MercadoPago;

public sealed class MercadoPagoPreferenceGateway : IMercadoPagoPreferenceGateway
{
    private readonly MercadoPagoOptions _options;

    public MercadoPagoPreferenceGateway(MercadoPagoOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<MercadoPagoPreferenceCreateResult> CreatePreferenceAsync(
        MercadoPagoPreferenceCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            MercadoPagoConfig.AccessToken = _options.AccessToken;

            var preferenceRequest = new PreferenceRequest
            {
                ExternalReference = request.ExternalReference,
                NotificationUrl = request.NotificationUrl,
                BackUrls = new PreferenceBackUrlsRequest
                {
                    Success = request.SuccessUrl,
                    Failure = request.FailureUrl,
                    Pending = request.PendingUrl
                },
                Items = request.Items.Select(x => new PreferenceItemRequest
                {
                    Title = x.Title,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice,
                    CurrencyId = x.CurrencyId
                }).ToList()
            };

            var client = new PreferenceClient();
            var preference = await client.CreateAsync(preferenceRequest);

            return new MercadoPagoPreferenceCreateResult(preference.Id, preference.InitPoint);
        }
        catch (Exception ex)
        {
            throw new PaymentPreferenceProviderException("Mercado Pago preference creation failed.", ex);
        }
    }
}
