using Application.Checkout.Contracts;
using Domain.Checkout;

namespace Application.Checkout.Services;

public sealed class CheckoutService
{
    private readonly ICartRepository _cartRepository;
    private readonly ICheckoutRepository _checkoutRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICheckoutInventoryGateway _inventoryGateway;
    private readonly ICheckoutProductCatalogGateway _catalogGateway;

    public CheckoutService(
        ICartRepository cartRepository,
        ICheckoutRepository checkoutRepository,
        ICustomerRepository customerRepository,
        ICheckoutInventoryGateway inventoryGateway,
        ICheckoutProductCatalogGateway catalogGateway)
    {
        _cartRepository = cartRepository ?? throw new ArgumentNullException(nameof(cartRepository));
        _checkoutRepository = checkoutRepository ?? throw new ArgumentNullException(nameof(checkoutRepository));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _inventoryGateway = inventoryGateway ?? throw new ArgumentNullException(nameof(inventoryGateway));
        _catalogGateway = catalogGateway ?? throw new ArgumentNullException(nameof(catalogGateway));
    }

    public async Task<SubmitCheckoutResponse> SubmitCheckoutAsync(
        SubmitCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var cart = await _cartRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException("Cart was not found for customer.");

        if (cart.Lines.Count == 0)
        {
            throw new InvalidOperationException("Cart is empty.");
        }

        var orderId = Guid.NewGuid();
        var orderIntentKey = $"checkout-{Guid.NewGuid():N}";
        var conflicts = new List<CheckoutLineConflict>();
        var hasSuccessfulReserve = false;
        var snapshotsByProductId = new Dictionary<Guid, CheckoutProductSnapshot>();

        foreach (var line in cart.Lines)
        {
            snapshotsByProductId[line.ProductId] = await _catalogGateway.GetSnapshotAsync(line.ProductId, cancellationToken);
        }

        foreach (var line in cart.Lines)
        {
            try
            {
                await _inventoryGateway.ReserveStockForCheckoutAsync(
                    orderId,
                    orderIntentKey,
                    line.ProductId,
                    line.Quantity,
                    cancellationToken);

                hasSuccessfulReserve = true;
            }
            catch (CheckoutReservationConflictException ex)
            {
                conflicts.AddRange(ex.LineConflicts);
            }
        }

        if (conflicts.Count > 0)
        {
            if (hasSuccessfulReserve)
            {
                await CompensateReservationsOrThrowAsync(orderIntentKey, cancellationToken);
            }

            throw new CheckoutReservationConflictException(conflicts);
        }

        var order = new Order(orderId, request.CustomerId, orderIntentKey);

        // D-08: Snapshot notification phone at order creation time (D-07, D-09, D-10)
        await SetNotificationMetadataAsync(order, request.CustomerId, cancellationToken);

        foreach (var line in cart.Lines)
        {
            var snapshot = snapshotsByProductId[line.ProductId];
            var instructionRequest = request.DeliveryInstructions.SingleOrDefault(x => x.ProductId == line.ProductId)
                ?? throw new ArgumentException($"Missing delivery instructions for product '{line.ProductId}'.", nameof(request));

            var instruction = CreateInstruction(snapshot.FulfillmentType, instructionRequest);

            order.AddItemSnapshot(new OrderItemSnapshot(
                snapshot.ProductId,
                line.Quantity,
                snapshot.UnitPrice,
                snapshot.Currency,
                snapshot.ProductName,
                snapshot.ProductSlug,
                snapshot.CategorySlug));

            order.AddDeliveryInstruction(instruction);
        }

        await _checkoutRepository.SaveOrderAsync(order, cancellationToken);
        await _cartRepository.ClearAsync(request.CustomerId, cancellationToken);

        return new SubmitCheckoutResponse(
            order.Id,
            order.OrderIntentKey,
            order.Items
                .Select(x => new CheckoutOrderItemResponse(
                    x.ProductId,
                    x.Quantity,
                    x.UnitPrice,
                    x.Currency,
                    x.ProductName,
                    x.ProductSlug,
                    x.CategorySlug))
                .ToList(),
            order.DeliveryInstructions
                .Select(x => new CheckoutDeliveryInstructionResponse(
                    x.ProductId,
                    x.FulfillmentType,
                    x.Status,
                    x.CompletedAtUtc,
                    x.TargetCharacter,
                    x.TargetServer,
                    x.DeliveryChannelOrContact,
                    x.RequestBrief,
                    x.ContactHandle))
                .ToList());
    }

    /// <summary>
    /// D-08: Snapshot notification phone from customer profile at checkout.
    /// Phone is validated for E.164 format; missing/invalid phone marks notification unavailable.
    /// </summary>
    private async Task SetNotificationMetadataAsync(Order order, Guid customerId, CancellationToken ct)
    {
        var phone = await _customerRepository.GetNotificationPhoneAsync(customerId, ct);
        var isValid = IsValidE164Phone(phone);
        order.SetNotificationMetadata(phone, isValid, isValid ? null : "missing-contact");
    }

    /// <summary>
    /// D-10: Validates phone is in E.164 format (+[country code][number]).
    /// </summary>
    private static bool IsValidE164Phone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        // E.164 format: starts with + followed by 1-15 digits
        return phone.StartsWith("+") && phone.Length >= 3 && phone.Length <= 16 
               && phone.Skip(1).All(char.IsDigit);
    }

    private async Task CompensateReservationsOrThrowAsync(string orderIntentKey, CancellationToken cancellationToken)
    {
        try
        {
            await _inventoryGateway.ReleaseCheckoutReservationAsync(
                orderIntentKey,
                Application.Inventory.Contracts.ReservationReleaseReason.OrderCanceled,
                cancellationToken);
        }
        catch (Exception ex)
        {
            throw new CheckoutReservationCompensationException(orderIntentKey, ex);
        }
    }

    private static DeliveryInstruction CreateInstruction(
        FulfillmentType fulfillmentType,
        CheckoutDeliveryInstructionRequest request)
    {
        return fulfillmentType switch
        {
            FulfillmentType.Automated => DeliveryInstruction.CreateAutomated(
                request.ProductId,
                request.TargetCharacter ?? string.Empty,
                request.TargetServer ?? string.Empty,
                request.DeliveryChannelOrContact ?? string.Empty),
            FulfillmentType.Manual => DeliveryInstruction.CreateManual(
                request.ProductId,
                request.RequestBrief ?? string.Empty,
                request.ContactHandle ?? string.Empty),
            _ => throw new ArgumentOutOfRangeException(nameof(fulfillmentType), fulfillmentType, "Unsupported fulfillment type.")
        };
    }

    private static void ValidateRequest(SubmitCheckoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.CustomerId == Guid.Empty)
        {
            throw new ArgumentException("Customer id is required.", nameof(request));
        }

        if (request.DeliveryInstructions is null)
        {
            throw new ArgumentException("Delivery instructions are required.", nameof(request));
        }
    }
}
