namespace Application.Checkout.Contracts;

public interface ICustomerRepository
{
    /// <summary>
    /// Gets the customer's notification phone number in E.164 format.
    /// Returns null if customer not found or phone not set.
    /// </summary>
    Task<string?> GetNotificationPhoneAsync(Guid customerId, CancellationToken cancellationToken = default);
}