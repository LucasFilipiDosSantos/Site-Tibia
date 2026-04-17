namespace Application.Checkout.Contracts;

public sealed record SubmitCheckoutRequest(
    Guid CustomerId,
    IReadOnlyList<CheckoutDeliveryInstructionRequest> DeliveryInstructions
);

public sealed record CheckoutDeliveryInstructionRequest(
    Guid ProductId,
    string? TargetCharacter,
    string? TargetServer,
    string? DeliveryChannelOrContact,
    string? RequestBrief,
    string? ContactHandle
);

public sealed record SubmitCheckoutResponse(
    Guid OrderId,
    string OrderIntentKey,
    IReadOnlyList<CheckoutOrderItemResponse> Items,
    IReadOnlyList<CheckoutDeliveryInstructionResponse> DeliveryInstructions
);

public sealed record CheckoutOrderItemResponse(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    string Currency,
    string ProductName,
    string ProductSlug,
    string CategorySlug
);

public sealed record CheckoutDeliveryInstructionResponse(
    Guid ProductId,
    Domain.Checkout.FulfillmentType FulfillmentType,
    string? TargetCharacter,
    string? TargetServer,
    string? DeliveryChannelOrContact,
    string? RequestBrief,
    string? ContactHandle
);

public sealed record CheckoutProductSnapshot(
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    string CategorySlug,
    decimal UnitPrice,
    string Currency,
    Domain.Checkout.FulfillmentType FulfillmentType
);

public sealed record CheckoutLineConflict(Guid ProductId, int RequestedQuantity, int AvailableQuantity);

public sealed class CheckoutReservationConflictException : InvalidOperationException
{
    public CheckoutReservationConflictException(IReadOnlyList<CheckoutLineConflict> lineConflicts)
        : base("Checkout cannot be completed because one or more cart lines exceed available stock.")
    {
        LineConflicts = lineConflicts;
    }

    public IReadOnlyList<CheckoutLineConflict> LineConflicts { get; }
}
