using Domain.Checkout;

namespace Application.Checkout.Contracts;

public interface ICheckoutRepository
{
    Task SaveOrderAsync(Order order, CancellationToken cancellationToken = default);

    Task<Order?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    
    // Per D-13: Admin search with filters
    Task<IReadOnlyList<Order>> SearchOrdersAsync(
        OrderStatus? status,
        Guid? customerId,
        DateTimeOffset? createdFromUtc,
        DateTimeOffset? createdToUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
