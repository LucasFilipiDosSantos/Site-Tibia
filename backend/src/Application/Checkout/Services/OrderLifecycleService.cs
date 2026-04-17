using Domain.Checkout;
using Application.Checkout.Contracts;

namespace Application.Checkout.Services;

public sealed class OrderLifecycleService
{
    private readonly IOrderLifecycleRepository _repository;

    public OrderLifecycleService(IOrderLifecycleRepository repository)
    {
        _repository = repository;
    }

    public async Task ApplySystemTransitionAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _repository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order {orderId} not found.");

        order.ApplyTransition(OrderStatus.Paid, TransitionSourceType.System, DateTimeOffset.UtcNow);
        await _repository.SaveAsync(order, cancellationToken);
    }

    public async Task ApplyCustomerCancelAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _repository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order {orderId} not found.");

        order.ApplyTransition(OrderStatus.Cancelled, TransitionSourceType.Customer, DateTimeOffset.UtcNow);
        await _repository.SaveAsync(order, cancellationToken);
    }

    public async Task ApplyAdminCancelAsync(Guid orderId, Guid actorUserId, string reason, CancellationToken cancellationToken = default)
    {
        var order = await _repository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order {orderId} not found.");

        order.ApplyTransition(
            OrderStatus.Cancelled,
            TransitionSourceType.Admin,
            DateTimeOffset.UtcNow,
            actorUserId,
            string.IsNullOrWhiteSpace(reason) ? null : reason);

        await _repository.SaveAsync(order, cancellationToken);
    }
}