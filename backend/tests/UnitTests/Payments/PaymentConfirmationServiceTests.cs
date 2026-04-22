using Application.Checkout.Contracts;
using Application.Checkout.Services;
using Application.Notifications;
using Application.Payments.Contracts;
using Application.Payments.Services;
using Domain.Checkout;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace UnitTests.Payments;

/// <summary>
/// Tests for PaymentConfirmationService status-to-lifecycle mapping (D-09 through D-12)
/// </summary>
public sealed class PaymentConfirmationServiceTests
{
    /// <summary>
    /// Mock repository tracking calls for verification
    /// </summary>
    private sealed class SpyOrderLifecycleRepository : IOrderLifecycleRepository
    {
        public Order? FetchedOrder { get; private set; }
        public bool SaveCalled { get; private set; }
        public OrderStatus? SavedStatus { get; private set; }
        public bool ReturnPaidStatus { get; set; }

        public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            // Return a Pending order for testing (or Paid if ReturnPaidStatus is set)
            FetchedOrder = ReturnPaidStatus
                ? new Order(orderId, Guid.NewGuid(), "test-key")
                : new Order(orderId, Guid.NewGuid(), "test-key");
            if (ReturnPaidStatus)
            {
                FetchedOrder.ApplyTransition(OrderStatus.Paid, TransitionSourceType.System, DateTimeOffset.UtcNow);
            }
            return Task.FromResult<Order?>(FetchedOrder);
        }

        public Task SaveAsync(Order order, CancellationToken cancellationToken = default)
        {
            SaveCalled = true;
            SavedStatus = order.Status;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Order>> GetCustomerOrdersAsync(Guid customerId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Order>>([]);
        }
    }

    /// <summary>
    /// Mock status event repository
    /// </summary>
    private sealed class SpyPaymentStatusEventRepository : IPaymentStatusEventRepository
    {
        public string ReturnStatus { get; set; } = "approved";

        public Task AddAsync(PaymentStatusEvent statusEvent, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<PaymentStatusEvent?> GetLatestAsync(string providerResourceId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PaymentStatusEvent?>(
                new PaymentStatusEvent(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    providerResourceId,
                    "payment." + ReturnStatus,
                    ReturnStatus,
                    DateTimeOffset.UtcNow,
                    null));
        }

        public Task<IReadOnlyList<PaymentStatusEvent>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PaymentStatusEvent>>([]);
        }
    }

    /// <summary>
    /// Mock payment link repository
    /// </summary>
    private sealed class SpyPaymentLinkRepository : IPaymentLinkRepository
    {
        public string? LastLookupPaymentId { get; private set; }
        public Guid ResolvedOrderId { get; set; } = Guid.NewGuid();

        public Task SaveAsync(PaymentLinkSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<PaymentLinkSnapshot?> GetByProviderPaymentIdAsync(
            string providerPaymentId,
            CancellationToken cancellationToken = default)
        {
            LastLookupPaymentId = providerPaymentId;
            return Task.FromResult<PaymentLinkSnapshot?>(
                new PaymentLinkSnapshot(
                    ResolvedOrderId,
                    providerPaymentId,
                    100.00m,
                    "BRL",
                    DateTimeOffset.UtcNow));
        }
    }

    [Fact]
    public async Task ApplyVerifiedConfirmation_ApprovedStatus_CallsLifecycleService()
    {
        // D-09: verified approved status should call lifecycle service
        var lifecycleRepo = new SpyOrderLifecycleRepository();
        var statusEventRepo = new SpyPaymentStatusEventRepository();
        var paymentLinkRepo = new SpyPaymentLinkRepository();
        var fulfillmentService = new MockFulfillmentService();
        var notificationPublisher = new MockNotificationPublisher();
        NullLogger<OrderLifecycleService> logger = NullLogger<OrderLifecycleService>.Instance;
        var lifecycleService = new OrderLifecycleService(lifecycleRepo, fulfillmentService, notificationPublisher, logger);
        
        var service = new PaymentConfirmationService(
            lifecycleRepo,
            statusEventRepo,
            paymentLinkRepo,
            lifecycleService,
            NullLogger<PaymentConfirmationService>.Instance);
        
        var result = await service.ApplyVerifiedConfirmationAsync("payment_123");
        
        Assert.True(result.IsSuccess);
        Assert.Equal(LifecycleTransitionDecision.MarkPaid, result.Decision);
    }

    [Fact]
    public async Task ApplyVerifiedConfirmation_ProcessedStatus_CallsLifecycleService()
    {
        // D-09: processed status should also call lifecycle service
        var lifecycleRepo = new SpyOrderLifecycleRepository();
        var statusEventRepo = new SpyPaymentStatusEventRepository() { ReturnStatus = "processed" };
        var paymentLinkRepo = new SpyPaymentLinkRepository();
        var fulfillmentService = new MockFulfillmentService();
        var notificationPublisher = new MockNotificationPublisher();
        NullLogger<OrderLifecycleService> logger = NullLogger<OrderLifecycleService>.Instance;
        var lifecycleService = new OrderLifecycleService(lifecycleRepo, fulfillmentService, notificationPublisher, logger);
        
        var service = new PaymentConfirmationService(
            lifecycleRepo,
            statusEventRepo,
            paymentLinkRepo,
            lifecycleService,
            NullLogger<PaymentConfirmationService>.Instance);
        
        var result = await service.ApplyVerifiedConfirmationAsync("payment_123");
        
        Assert.True(result.IsSuccess);
        Assert.Equal(LifecycleTransitionDecision.MarkPaid, result.Decision);
    }

    [Fact]
    public async Task ApplyVerifiedConfirmation_PendingStatus_KeepsOrderPending()
    {
        // D-10: pending status should NOT transition to paid
        var lifecycleRepo = new SpyOrderLifecycleRepository();
        var statusEventRepo = new SpyPaymentStatusEventRepository() { ReturnStatus = "pending" };
        var paymentLinkRepo = new SpyPaymentLinkRepository();
        var fulfillmentService = new MockFulfillmentService();
        var notificationPublisher = new MockNotificationPublisher();
        NullLogger<OrderLifecycleService> logger = NullLogger<OrderLifecycleService>.Instance;
        var lifecycleService = new OrderLifecycleService(lifecycleRepo, fulfillmentService, notificationPublisher, logger);
        
        var service = new PaymentConfirmationService(
            lifecycleRepo,
            statusEventRepo,
            paymentLinkRepo,
            lifecycleService,
            NullLogger<PaymentConfirmationService>.Instance);
        
        var result = await service.ApplyVerifiedConfirmationAsync("payment_123");
        
        Assert.True(result.IsSuccess);
        Assert.Equal(LifecycleTransitionDecision.KeepPending, result.Decision);
    }

    [Fact]
    public async Task ApplyVerifiedConfirmation_AuthorizedStatus_KeepsOrderPending()
    {
        // D-10: authorized status keeps order Pending
        var lifecycleRepo = new SpyOrderLifecycleRepository();
        var statusEventRepo = new SpyPaymentStatusEventRepository() { ReturnStatus = "authorized" };
        var paymentLinkRepo = new SpyPaymentLinkRepository();
        var fulfillmentService = new MockFulfillmentService();
        var notificationPublisher = new MockNotificationPublisher();
        NullLogger<OrderLifecycleService> logger = NullLogger<OrderLifecycleService>.Instance;
        var lifecycleService = new OrderLifecycleService(lifecycleRepo, fulfillmentService, notificationPublisher, logger);
        
        var service = new PaymentConfirmationService(
            lifecycleRepo,
            statusEventRepo,
            paymentLinkRepo,
            lifecycleService,
            NullLogger<PaymentConfirmationService>.Instance);
        
        var result = await service.ApplyVerifiedConfirmationAsync("payment_123");
        
        Assert.True(result.IsSuccess);
        Assert.Equal(LifecycleTransitionDecision.KeepPending, result.Decision);
    }

    [Fact]
    public async Task ApplyVerifiedConfirmation_RejectedStatus_DoesNotMarkPaid()
    {
        // D-11: rejected status does NOT transition to paid
        var lifecycleRepo = new SpyOrderLifecycleRepository();
        var statusEventRepo = new SpyPaymentStatusEventRepository() { ReturnStatus = "rejected" };
        var paymentLinkRepo = new SpyPaymentLinkRepository();
        var fulfillmentService = new MockFulfillmentService();
        var notificationPublisher = new MockNotificationPublisher();
        NullLogger<OrderLifecycleService> logger = NullLogger<OrderLifecycleService>.Instance;
        var lifecycleService = new OrderLifecycleService(lifecycleRepo, fulfillmentService, notificationPublisher, logger);
        
        var service = new PaymentConfirmationService(
            lifecycleRepo,
            statusEventRepo,
            paymentLinkRepo,
            lifecycleService,
            NullLogger<PaymentConfirmationService>.Instance);
        
        var result = await service.ApplyVerifiedConfirmationAsync("payment_123");
        
        Assert.True(result.IsSuccess);
        Assert.Equal(LifecycleTransitionDecision.NoPaidTransition, result.Decision);
    }

    [Fact]
    public async Task ApplyVerifiedConfirmation_CancelledStatus_DoesNotMarkPaid()
    {
        // D-11: cancelled does NOT transition to paid
        var lifecycleRepo = new SpyOrderLifecycleRepository();
        var statusEventRepo = new SpyPaymentStatusEventRepository() { ReturnStatus = "cancelled" };
        var paymentLinkRepo = new SpyPaymentLinkRepository();
        var fulfillmentService = new MockFulfillmentService();
        var notificationPublisher = new MockNotificationPublisher();
        NullLogger<OrderLifecycleService> logger = NullLogger<OrderLifecycleService>.Instance;
        var lifecycleService = new OrderLifecycleService(lifecycleRepo, fulfillmentService, notificationPublisher, logger);
        
        var service = new PaymentConfirmationService(
            lifecycleRepo,
            statusEventRepo,
            paymentLinkRepo,
            lifecycleService,
            NullLogger<PaymentConfirmationService>.Instance);
        
        var result = await service.ApplyVerifiedConfirmationAsync("payment_123");
        
        Assert.True(result.IsSuccess);
        Assert.Equal(LifecycleTransitionDecision.NoPaidTransition, result.Decision);
    }

    [Fact]
    public async Task ApplyVerifiedConfirmation_RefundedStatus_DoesNotMarkPaid()
    {
        // D-11: refunded does NOT transition to paid
        var lifecycleRepo = new SpyOrderLifecycleRepository();
        var statusEventRepo = new SpyPaymentStatusEventRepository() { ReturnStatus = "refunded" };
        var paymentLinkRepo = new SpyPaymentLinkRepository();
        var fulfillmentService = new MockFulfillmentService();
        var notificationPublisher = new MockNotificationPublisher();
        NullLogger<OrderLifecycleService> logger = NullLogger<OrderLifecycleService>.Instance;
        var lifecycleService = new OrderLifecycleService(lifecycleRepo, fulfillmentService, notificationPublisher, logger);
        
        var service = new PaymentConfirmationService(
            lifecycleRepo,
            statusEventRepo,
            paymentLinkRepo,
            lifecycleService,
            NullLogger<PaymentConfirmationService>.Instance);
        
        var result = await service.ApplyVerifiedConfirmationAsync("payment_123");
        
        Assert.True(result.IsSuccess);
        Assert.Equal(LifecycleTransitionDecision.NoPaidTransition, result.Decision);
    }

    /// <summary>
    /// Mock IFulfillmentService for testing.
    /// </summary>
    private sealed class MockFulfillmentService : IFulfillmentService
    {
        public Task RouteFulfillmentAsync(Guid orderId, string? correlationId = null, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Mock INotificationPublisher for testing.
    /// </summary>
    private sealed class MockNotificationPublisher : INotificationPublisher
    {
        public Task PublishOrderPaidAsync(Order order, DateTimeOffset statusAtUtc, string? correlationId = null, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task PublishDeliveryCompletedAsync(Order order, DateTimeOffset statusAtUtc, string? correlationId = null, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task PublishDeliveryFailedAsync(Order order, string failureReason, DateTimeOffset statusAtUtc, string? correlationId = null, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }
}
