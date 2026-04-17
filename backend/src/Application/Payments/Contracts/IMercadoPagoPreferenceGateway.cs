namespace Application.Payments.Contracts;

public interface IMercadoPagoPreferenceGateway
{
    Task<MercadoPagoPreferenceCreateResult> CreatePreferenceAsync(
        MercadoPagoPreferenceCreateRequest request,
        CancellationToken cancellationToken = default);
}
