using Domain.Checkout;

namespace Application.Checkout.Contracts;

public interface ICheckoutRepository
{
    Task SaveOrderAsync(Order order, CancellationToken cancellationToken = default);

    Task<Order?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}
