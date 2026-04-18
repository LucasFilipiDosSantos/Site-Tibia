namespace Application.Payments.Contracts;

/// <summary>
/// Lifecycle transition decision from payment confirmation processing (D-09 through D-12)
/// </summary>
public enum LifecycleTransitionDecision
{
    /// <summary>
    /// D-09: Approved/processed payment should mark order as Paid
    /// </summary>
    MarkPaid,

    /// <summary>
    /// D-10: Non-approved pending statuses should keep order Pending
    /// </summary>
    KeepPending,

    /// <summary>
    /// D-12: Order already paid - lifecycle no-op
    /// </summary>
    AlreadyPaidNoOp,

    /// <summary>
    /// D-11: Invalid or rejected - no transition
    /// </summary>
    NoPaidTransition
}

/// <summary>
/// Result of applying a verified payment confirmation
/// </summary>
public sealed class PaymentConfirmationResult
{
    public bool IsSuccess { get; private set; }
    public LifecycleTransitionDecision Decision { get; private set; }
    public string? FailureReason { get; private set; }

    private PaymentConfirmationResult() { }

    public static PaymentConfirmationResult MarkedPaid() => new()
    {
        IsSuccess = true,
        Decision = LifecycleTransitionDecision.MarkPaid
    };

    public static PaymentConfirmationResult KeptPending() => new()
    {
        IsSuccess = true,
        Decision = LifecycleTransitionDecision.KeepPending
    };

    public static PaymentConfirmationResult AlreadyPaidNoOp() => new()
    {
        IsSuccess = true,
        Decision = LifecycleTransitionDecision.AlreadyPaidNoOp
    };

    public static PaymentConfirmationResult NoTransition() => new()
    {
        IsSuccess = true,
        Decision = LifecycleTransitionDecision.NoPaidTransition
    };

    public static PaymentConfirmationResult Failed(string reason) => new()
    {
        IsSuccess = false,
        Decision = LifecycleTransitionDecision.NoPaidTransition,
        FailureReason = reason
    };
}