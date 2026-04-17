using Application.Checkout.Contracts;
using Application.Payments.Contracts;
using Application.Payments.Services;
using Domain.Checkout;

namespace UnitTests.Payments;

public sealed class PaymentPreferenceServiceTests
{
    [Fact]
    public async Task CreatePreferenceAsync_UsesOrderIdAsExternalReference_AndPersistsSnapshot()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var order = BuildOrder(orderId, customerId, 2, 19.99m, "BRL");
        var checkoutRepository = new InMemoryCheckoutRepository(order);
        var gateway = new CapturingPreferenceGateway();
        var paymentLinkRepository = new InMemoryPaymentLinkRepository();
        var sut = new PaymentPreferenceService(
            checkoutRepository,
            gateway,
            paymentLinkRepository,
            new PaymentPreferenceSettings(
                "https://example.com/webhook",
                "https://example.com/success",
                "https://example.com/failure",
                "https://example.com/pending"));

        var response = await sut.CreatePreferenceAsync(orderId, customerId);

        Assert.Equal(orderId.ToString(), gateway.LastRequest!.ExternalReference);
        Assert.Equal(orderId.ToString(), response.ExternalReference);

        Assert.Equal(39.98m, paymentLinkRepository.LastSaved!.ExpectedAmount);
        Assert.Equal("BRL", paymentLinkRepository.LastSaved.ExpectedCurrency);
        Assert.Equal(response.PreferenceId, paymentLinkRepository.LastSaved.PreferenceId);
        Assert.Equal(orderId, paymentLinkRepository.LastSaved.OrderId);
    }

    [Fact]
    public async Task CreatePreferenceAsync_WhenOrderBelongsToDifferentCustomer_ThrowsNotFound()
    {
        var orderId = Guid.NewGuid();
        var order = BuildOrder(orderId, Guid.NewGuid(), 1, 10m, "BRL");
        var checkoutRepository = new InMemoryCheckoutRepository(order);
        var gateway = new CapturingPreferenceGateway();
        var paymentLinkRepository = new InMemoryPaymentLinkRepository();
        var sut = new PaymentPreferenceService(
            checkoutRepository,
            gateway,
            paymentLinkRepository,
            new PaymentPreferenceSettings(
                "https://example.com/webhook",
                "https://example.com/success",
                "https://example.com/failure",
                "https://example.com/pending"));

        await Assert.ThrowsAsync<PaymentPreferenceOrderNotFoundException>(() =>
            sut.CreatePreferenceAsync(orderId, Guid.NewGuid()));

        Assert.Null(gateway.LastRequest);
        Assert.Null(paymentLinkRepository.LastSaved);
    }

    private static Order BuildOrder(Guid orderId, Guid customerId, int quantity, decimal unitPrice, string currency)
    {
        var order = new Order(orderId, customerId, $"checkout-{Guid.NewGuid():N}");
        order.AddItemSnapshot(new OrderItemSnapshot(
            Guid.NewGuid(),
            quantity,
            unitPrice,
            currency,
            "Gold Package",
            "gold-package",
            "gold"));

        return order;
    }

    private sealed class InMemoryCheckoutRepository : ICheckoutRepository
    {
        private readonly Order _order;

        public InMemoryCheckoutRepository(Order order)
        {
            _order = order;
        }

        public Task SaveOrderAsync(Order order, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<Order?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
            => Task.FromResult(_order.Id == orderId ? _order : null);

        public Task<IReadOnlyList<Order>> SearchOrdersAsync(
            OrderStatus? status,
            Guid? customerId,
            DateTimeOffset? createdFromUtc,
            DateTimeOffset? createdToUtc,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Order>>([]);
    }

    private sealed class CapturingPreferenceGateway : IMercadoPagoPreferenceGateway
    {
        public MercadoPagoPreferenceCreateRequest? LastRequest { get; private set; }

        public Task<MercadoPagoPreferenceCreateResult> CreatePreferenceAsync(
            MercadoPagoPreferenceCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(new MercadoPagoPreferenceCreateResult("pref_123", "https://example.com/init"));
        }
    }

    private sealed class InMemoryPaymentLinkRepository : IPaymentLinkRepository
    {
        public PaymentLinkSnapshot? LastSaved { get; private set; }

        public Task SaveAsync(PaymentLinkSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            LastSaved = snapshot;
            return Task.CompletedTask;
        }
    }
}
