using Domain.Checkout;

namespace Application.Checkout.Contracts;

public interface IOrderLifecycleRepository
{
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task SaveAsync(Order order, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetCustomerOrdersAsync(Guid customerId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> HasPaidOrderForProductAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReviewOrderDiagnostic>> GetReviewOrderDiagnosticsAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default);
}

public sealed record ReviewOrderDiagnostic(
    Guid OrderId,
    string OrderIntentKey,
    OrderStatus Status,
    bool IsHidden,
    int ItemCount,
    IReadOnlyList<ReviewOrderItemDiagnostic> Items);

public sealed record ReviewOrderItemDiagnostic(Guid ProductId, string ProductSlug);
