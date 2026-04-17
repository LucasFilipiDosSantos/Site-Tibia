namespace API.Checkout;

public sealed record AddCartItemDto(Guid ProductId, int Quantity);

public sealed record SetCartItemQuantityDto(int Quantity);

public sealed record CartLineDto(Guid ProductId, int Quantity);

public sealed record CartResponseDto(Guid CustomerId, IReadOnlyList<CartLineDto> Lines);

public sealed record CheckoutDeliveryInstructionDto(
    Guid ProductId,
    string? TargetCharacter,
    string? TargetServer,
    string? DeliveryChannelOrContact,
    string? RequestBrief,
    string? ContactHandle
);

public sealed record SubmitCheckoutDto(IReadOnlyList<CheckoutDeliveryInstructionDto> DeliveryInstructions);

public sealed record CheckoutOrderItemDto(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    string Currency,
    string ProductName,
    string ProductSlug,
    string CategorySlug
);

public sealed record CheckoutDeliveryInstructionResponseDto(
    Guid ProductId,
    string FulfillmentType,
    string? TargetCharacter,
    string? TargetServer,
    string? DeliveryChannelOrContact,
    string? RequestBrief,
    string? ContactHandle
);

public sealed record SubmitCheckoutResponseDto(
    Guid OrderId,
    string OrderIntentKey,
    IReadOnlyList<CheckoutOrderItemDto> Items,
    IReadOnlyList<CheckoutDeliveryInstructionResponseDto> DeliveryInstructions
);

public sealed record OrderResponseDto(
    Guid OrderId,
    Guid CustomerId,
    string OrderIntentKey,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<CheckoutOrderItemDto> Items,
    IReadOnlyList<CheckoutDeliveryInstructionResponseDto> DeliveryInstructions
);
