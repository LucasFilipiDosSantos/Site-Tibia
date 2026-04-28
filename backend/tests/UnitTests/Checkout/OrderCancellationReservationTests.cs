using Application.Checkout.Contracts;
using Application.Checkout.Services;
using Application.Inventory.Contracts;
using Application.Notifications;
using Application.Payments.Contracts;
using Application.Payments.Services;
using Domain.Checkout;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests.Checkout;

public sealed class OrderCancellationReservationTests
{
    [Fact]
    public async Task ApplyAdminCancelAsync_ReleasesCheckoutReservation()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "checkout-reserved");
        var repository = new InMemoryOrderLifecycleRepository(order);
        var inventoryGateway = new SpyCheckoutInventoryGateway();
        var service = CreateLifecycleService(repository, inventoryGateway);

        await service.ApplyAdminCancelAsync(order.Id, Guid.NewGuid(), "Cliente desistiu");

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("checkout-reserved", inventoryGateway.LastReleasedIntentKey);
        Assert.Equal(ReservationReleaseReason.OrderCanceled, inventoryGateway.LastReleaseReason);
    }

    [Fact]
    public async Task ApplySystemCancelAsync_WhenOrderIsPending_CancelsAndReleasesCheckoutReservation()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "checkout-payment-cancelled");
        var repository = new InMemoryOrderLifecycleRepository(order);
        var inventoryGateway = new SpyCheckoutInventoryGateway();
        var service = CreateLifecycleService(repository, inventoryGateway);

        await service.ApplySystemCancelAsync(order.Id, "payment-cancelled");

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("checkout-payment-cancelled", inventoryGateway.LastReleasedIntentKey);
        Assert.Equal(ReservationReleaseReason.OrderCanceled, inventoryGateway.LastReleaseReason);
    }

    [Fact]
    public async Task PaymentCancelled_CancelsPendingOrderAndReleasesCheckoutReservation()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "checkout-payment-cancelled");
        var repository = new InMemoryOrderLifecycleRepository(order);
        var inventoryGateway = new SpyCheckoutInventoryGateway();
        var lifecycleService = CreateLifecycleService(repository, inventoryGateway);
        var service = new PaymentConfirmationService(
            repository,
            new StaticPaymentStatusEventRepository(order.Id, "cancelled"),
            new StaticPaymentLinkRepository(order.Id),
            lifecycleService,
            NullLogger<PaymentConfirmationService>.Instance);

        var result = await service.ApplyVerifiedConfirmationAsync("payment-123");

        Assert.True(result.IsSuccess);
        Assert.Equal(LifecycleTransitionDecision.NoPaidTransition, result.Decision);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("checkout-payment-cancelled", inventoryGateway.LastReleasedIntentKey);
    }

    private static OrderLifecycleService CreateLifecycleService(
        IOrderLifecycleRepository repository,
        ICheckoutInventoryGateway inventoryGateway)
    {
        return new OrderLifecycleService(
            repository,
            new NoOpFulfillmentService(),
            new NoOpNotificationPublisher(),
            NullLogger<OrderLifecycleService>.Instance,
            inventoryGateway);
    }

    private sealed class InMemoryOrderLifecycleRepository : IOrderLifecycleRepository
    {
        private readonly Order _order;

        public InMemoryOrderLifecycleRepository(Order order)
        {
            _order = order;
        }

        public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
            => Task.FromResult(orderId == _order.Id ? _order : null);

        public Task SaveAsync(Order order, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<Order>> GetCustomerOrdersAsync(Guid customerId, string? customerEmail, int page, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Order>>([]);

        public Task<bool> HasPaidOrderForProductAsync(Guid customerId, string? customerEmail, Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<IReadOnlyList<ReviewOrderDiagnostic>> GetReviewOrderDiagnosticsAsync(Guid customerId, string? customerEmail, Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ReviewOrderDiagnostic>>([]);
    }

    private sealed class SpyCheckoutInventoryGateway : ICheckoutInventoryGateway
    {
        public string? LastReleasedIntentKey { get; private set; }
        public ReservationReleaseReason? LastReleaseReason { get; private set; }

        public Task ReserveStockForCheckoutAsync(
            Guid orderId,
            string orderIntentKey,
            Guid productId,
            int quantity,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task ReleaseCheckoutReservationAsync(
            string orderIntentKey,
            ReservationReleaseReason reason,
            CancellationToken cancellationToken = default)
        {
            LastReleasedIntentKey = orderIntentKey;
            LastReleaseReason = reason;
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpFulfillmentService : IFulfillmentService
    {
        public Task RouteFulfillmentAsync(Guid orderId, string? correlationId = null, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class NoOpNotificationPublisher : INotificationPublisher
    {
        public Task PublishOrderPaidAsync(Order order, DateTimeOffset statusAtUtc, string? correlationId = null, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task PublishDeliveryCompletedAsync(Order order, DateTimeOffset statusAtUtc, string? correlationId = null, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task PublishDeliveryFailedAsync(Order order, string failureReason, DateTimeOffset statusAtUtc, string? correlationId = null, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class StaticPaymentStatusEventRepository : IPaymentStatusEventRepository
    {
        private readonly Guid _orderId;
        private readonly string _status;

        public StaticPaymentStatusEventRepository(Guid orderId, string status)
        {
            _orderId = orderId;
            _status = status;
        }

        public Task AddAsync(PaymentStatusEvent statusEvent, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<PaymentStatusEvent?> GetLatestAsync(string providerResourceId, CancellationToken cancellationToken = default)
            => Task.FromResult<PaymentStatusEvent?>(new PaymentStatusEvent(
                Guid.NewGuid(),
                _orderId,
                providerResourceId,
                $"payment.{_status}",
                _status,
                DateTimeOffset.UtcNow,
                null));

        public Task<IReadOnlyList<PaymentStatusEvent>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PaymentStatusEvent>>([]);
    }

    private sealed class StaticPaymentLinkRepository : IPaymentLinkRepository
    {
        private readonly Guid _orderId;

        public StaticPaymentLinkRepository(Guid orderId)
        {
            _orderId = orderId;
        }

        public Task SaveAsync(PaymentLinkSnapshot snapshot, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<PaymentLinkSnapshot?> GetByProviderPaymentIdAsync(
            string providerPaymentId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<PaymentLinkSnapshot?>(new PaymentLinkSnapshot(
                _orderId,
                providerPaymentId,
                100m,
                "BRL",
                DateTimeOffset.UtcNow));
    }
}
