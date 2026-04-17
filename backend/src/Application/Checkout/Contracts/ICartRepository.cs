using Domain.Checkout;

namespace Application.Checkout.Contracts;

public interface ICartRepository
{
    Task<Cart?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task SaveAsync(Cart cart, CancellationToken cancellationToken = default);

    Task ClearAsync(Guid customerId, CancellationToken cancellationToken = default);
}
