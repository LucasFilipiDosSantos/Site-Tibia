using Domain.Checkout;

namespace Application.Checkout.Contracts;

public interface IOrderLifecycleRepository
{
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task SaveAsync(Order order, CancellationToken cancellationToken = default);
}