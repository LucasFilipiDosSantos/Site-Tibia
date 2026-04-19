using Application.Checkout.Contracts;
using Application.Checkout.Services;
using Application.Payments.Contracts;
using Domain.Checkout;
using Microsoft.Extensions.Logging;

namespace Application.Payments.Services;

/// <summary>
/// Service that maps verified payment statuses to order lifecycle transitions (D-09 through D-12)
/// </summary>
public sealed class PaymentConfirmationService
{
    private readonly IOrderLifecycleRepository _repository;
    private readonly IPaymentStatusEventRepository _statusEventRepository;
    private readonly IPaymentLinkRepository _paymentLinkRepository;
    private readonly OrderLifecycleService _lifecycleService;
    private readonly ILogger<PaymentConfirmationService> _logger;

    public PaymentConfirmationService(
        IOrderLifecycleRepository repository,
        IPaymentStatusEventRepository statusEventRepository,
        IPaymentLinkRepository paymentLinkRepository,
        OrderLifecycleService lifecycleService,
        ILogger<PaymentConfirmationService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _statusEventRepository = statusEventRepository ?? throw new ArgumentNullException(nameof(statusEventRepository));
        _paymentLinkRepository = paymentLinkRepository ?? throw new ArgumentNullException(nameof(paymentLinkRepository));
        _lifecycleService = lifecycleService ?? throw new ArgumentNullException(nameof(lifecycleService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Apply a verified payment confirmation, mapping status to lifecycle decision per D-09 to D-12
    /// D-14: Correlation spans full chain.
    /// </summary>
    /// <param name="providerPaymentId">The Mercado Pago payment ID (provider resource ID)</param>
    /// <param name="correlationId">Correlation ID for full chain observability</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with lifecycle transition decision</returns>
    public async Task<PaymentConfirmationResult> ApplyVerifiedConfirmationAsync(
        string providerPaymentId,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        // Resolve orderId from payment link (D-01 binding via external_reference)
        var paymentLink = await _paymentLinkRepository.GetByProviderPaymentIdAsync(providerPaymentId, cancellationToken);
        if (paymentLink == null)
        {
            _logger.LogWarning("No payment link found for provider payment {ProviderPaymentId}", providerPaymentId);
            return PaymentConfirmationResult.Failed("Payment link not found");
        }

        var orderId = paymentLink.OrderId;

        // Get latest payment status event for this payment
        var latestStatusEvent = await _statusEventRepository.GetLatestAsync(providerPaymentId, cancellationToken);
        if (latestStatusEvent == null)
        {
            _logger.LogWarning("No payment status event found for provider payment {ProviderPaymentId}", providerPaymentId);
            return PaymentConfirmationResult.Failed("Payment status event not found");
        }

        // Map status to lifecycle decision (D-09 through D-12)
        var decision = MapStatusToLifecycleDecision(latestStatusEvent.Status);

        switch (decision)
        {
            case LifecycleTransitionDecision.MarkPaid:
                return await ApplyMarkPaidTransitionAsync(orderId, providerPaymentId, correlationId, cancellationToken);

            case LifecycleTransitionDecision.KeepPending:
                _logger.LogInformation(
                    "Payment status {Status} for order {OrderId} keeps order Pending",
                    latestStatusEvent.Status,
                    orderId);
                return PaymentConfirmationResult.KeptPending();

            case LifecycleTransitionDecision.NoPaidTransition:
            case LifecycleTransitionDecision.AlreadyPaidNoOp:
                // D-11: rejected, cancelled, refunded, unknown, invalid signature
                _logger.LogInformation(
                    "Payment status {Status} for order {OrderId} does not transition to Paid",
                    latestStatusEvent.Status,
                    orderId);
                return decision == LifecycleTransitionDecision.AlreadyPaidNoOp
                    ? PaymentConfirmationResult.AlreadyPaidNoOp()
                    : PaymentConfirmationResult.NoTransition();

            default:
                return PaymentConfirmationResult.NoTransition();
        }
    }

    /// <summary>
    /// D-09: Map verified payment status to lifecycle transition decision
    /// </summary>
    private static LifecycleTransitionDecision MapStatusToLifecycleDecision(string status)
    {
        // D-09: Only verified approved or processed marks as Paid
        if (status.Equals("approved", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("processed", StringComparison.OrdinalIgnoreCase))
        {
            return LifecycleTransitionDecision.MarkPaid;
        }

        // D-10: pending, in_process, authorized keep order Pending
        if (status.Equals("pending", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("in_process", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("authorized", StringComparison.OrdinalIgnoreCase))
        {
            return LifecycleTransitionDecision.KeepPending;
        }

        // D-11: rejected, cancelled, refunded, unknown never mark as Paid
        // Also covers invalid signature case
        return LifecycleTransitionDecision.NoPaidTransition;
    }

    /// <summary>
    /// D-09: Apply Paid transition via lifecycle service
    /// D-12: Handle already-paid idempotent no-op
    /// D-14: Pass correlation ID through chain
    /// </summary>
    private async Task<PaymentConfirmationResult> ApplyMarkPaidTransitionAsync(
        Guid orderId,
        string providerPaymentId,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            _logger.LogError("Order {OrderId} not found for payment {ProviderPaymentId}", orderId, providerPaymentId);
            return PaymentConfirmationResult.Failed($"Order {orderId} not found");
        }

        // D-12: If already Paid, treat as lifecycle no-op (no duplicate transition, no timeline inflation)
        if (order.Status == OrderStatus.Paid)
        {
            _logger.LogInformation(
                "Order {OrderId} already Paid - lifecycle no-op for payment {ProviderPaymentId}",
                orderId,
                providerPaymentId);
            return PaymentConfirmationResult.AlreadyPaidNoOp();
        }

        // D-09: Apply system transition through lifecycle service (not direct order.ApplyTransition)
        // D-14: Pass correlation ID through chain
        await _lifecycleService.ApplySystemTransitionAsync(orderId, correlationId, cancellationToken);

        _logger.LogInformation(
            "Order {OrderId} transitioned to Paid via verified payment {ProviderPaymentId}, correlation ID: {CorrelationId}",
            orderId,
            providerPaymentId,
            correlationId);

        return PaymentConfirmationResult.MarkedPaid();
    }
}