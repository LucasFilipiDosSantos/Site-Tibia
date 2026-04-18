namespace Application.Payments.Contracts;

public interface IPaymentLinkRepository
{
    Task SaveAsync(PaymentLinkSnapshot snapshot, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get payment link by provider payment/resource ID for lifecycle resolution
    /// </summary>
    Task<PaymentLinkSnapshot?> GetByProviderPaymentIdAsync(string providerPaymentId, CancellationToken cancellationToken = default);
}
