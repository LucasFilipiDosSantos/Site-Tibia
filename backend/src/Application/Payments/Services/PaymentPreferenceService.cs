using Application.Checkout.Contracts;
using Application.Payments.Contracts;

namespace Application.Payments.Services;

public sealed class PaymentPreferenceService
{
    private readonly ICheckoutRepository _checkoutRepository;
    private readonly IMercadoPagoPreferenceGateway _preferenceGateway;
    private readonly IPaymentLinkRepository _paymentLinkRepository;
    private readonly PaymentPreferenceSettings _settings;

    public PaymentPreferenceService(
        ICheckoutRepository checkoutRepository,
        IMercadoPagoPreferenceGateway preferenceGateway,
        IPaymentLinkRepository paymentLinkRepository,
        PaymentPreferenceSettings settings)
    {
        _checkoutRepository = checkoutRepository ?? throw new ArgumentNullException(nameof(checkoutRepository));
        _preferenceGateway = preferenceGateway ?? throw new ArgumentNullException(nameof(preferenceGateway));
        _paymentLinkRepository = paymentLinkRepository ?? throw new ArgumentNullException(nameof(paymentLinkRepository));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<CreatePaymentPreferenceResponse> CreatePreferenceAsync(
        Guid orderId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order id is required.", nameof(orderId));
        }

        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("Customer id is required.", nameof(customerId));
        }

        var order = await _checkoutRepository.GetOrderByIdAsync(orderId, cancellationToken);
        if (order is null || order.CustomerId != customerId)
        {
            throw new PaymentPreferenceOrderNotFoundException(orderId);
        }

        if (order.Items.Count == 0)
        {
            throw new InvalidOperationException("Order has no item snapshots to create payment preference.");
        }

        var distinctCurrencies = order.Items.Select(x => x.Currency).Distinct(StringComparer.Ordinal).ToList();
        if (distinctCurrencies.Count != 1)
        {
            throw new InvalidOperationException("Order item snapshots must have exactly one currency.");
        }

        var externalReference = orderId.ToString();
        var currency = distinctCurrencies[0];
        var expectedAmount = order.Items.Sum(x => x.UnitPrice * x.Quantity);

        var preferenceRequest = new MercadoPagoPreferenceCreateRequest(
            ExternalReference: externalReference,
            NotificationUrl: _settings.NotificationUrl,
            SuccessUrl: _settings.SuccessUrl,
            FailureUrl: _settings.FailureUrl,
            PendingUrl: _settings.PendingUrl,
            Items: order.Items.Select(x => new PaymentPreferenceItem(
                x.ProductName,
                x.Quantity,
                x.UnitPrice,
                x.Currency)).ToList());

        var preference = await _preferenceGateway.CreatePreferenceAsync(preferenceRequest, cancellationToken);

        await _paymentLinkRepository.SaveAsync(
            new PaymentLinkSnapshot(
                order.Id,
                preference.PreferenceId,
                expectedAmount,
                currency,
                DateTimeOffset.UtcNow),
            cancellationToken);

        return new CreatePaymentPreferenceResponse(
            preference.PreferenceId,
            preference.InitPointUrl,
            externalReference);
    }
}
