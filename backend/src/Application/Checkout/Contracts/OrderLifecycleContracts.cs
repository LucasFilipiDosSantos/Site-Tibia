namespace Application.Checkout.Contracts;

public class ForbiddenStatusTransitionException : InvalidOperationException
{
    public ForbiddenStatusTransitionException(
        Domain.Checkout.OrderStatus currentStatus,
        IReadOnlyList<Domain.Checkout.OrderStatus> allowedTransitions,
        Domain.Checkout.OrderStatus attemptedTransition,
        Domain.Checkout.TransitionSourceType source)
        : base($"Cannot transition from {currentStatus} to {attemptedTransition} by {source}.")
    {
        CurrentStatus = currentStatus;
        AllowedTransitions = allowedTransitions;
        AttemptedTransition = attemptedTransition;
        SourceType = source;
    }

    public Domain.Checkout.OrderStatus CurrentStatus { get; }
    public IReadOnlyList<Domain.Checkout.OrderStatus> AllowedTransitions { get; }
    public Domain.Checkout.OrderStatus AttemptedTransition { get; }
    public Domain.Checkout.TransitionSourceType SourceType { get; }
}

public sealed record ApplySystemTransitionRequest(Guid OrderId);

public sealed record ApplyCustomerCancelRequest(Guid OrderId);

public sealed record ApplyAdminCancelRequest(Guid OrderId, string Reason);