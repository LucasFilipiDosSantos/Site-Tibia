namespace Application.Payments.Contracts;

public interface IPaymentLinkRepository
{
    Task SaveAsync(PaymentLinkSnapshot snapshot, CancellationToken cancellationToken = default);
}
