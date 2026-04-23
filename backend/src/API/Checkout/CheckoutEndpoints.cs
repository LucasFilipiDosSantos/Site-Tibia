using API.Auth;
using Application.Catalog.Contracts;
using Application.Checkout.Contracts;
using Application.Checkout.Services;
using Application.Payments.Contracts;
using Application.Payments.Services;
using Domain.Checkout;
using System.Security.Claims;

namespace API.Checkout;

public static class CheckoutEndpoints
{
    public static IEndpointRouteBuilder MapCheckoutEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/orders/support-pending", async (
            SupportPendingCheckoutDto request,
            IProductRepository productRepository,
            ICheckoutRepository checkoutRepository,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
            {
                return Results.BadRequest(new { message = "Nome e e-mail sao obrigatorios." });
            }

            if (request.Items.Count == 0)
            {
                return Results.BadRequest(new { message = "O carrinho esta vazio." });
            }

            var order = new Order(Guid.NewGuid(), Guid.NewGuid(), $"support-{Guid.NewGuid():N}");
            order.SetCustomerContact(request.Name, request.Email, request.Discord, request.PaymentMethod);
            order.SetNotificationMetadata(null, available: false, failedReason: "support-checkout");

            foreach (var item in request.Items)
            {
                if (item.Quantity <= 0)
                {
                    return Results.BadRequest(new { message = "Quantidade invalida no carrinho." });
                }

                var slug = item.ProductId.Trim().ToLowerInvariant();
                var product = await productRepository.GetBySlugAsync(slug, ct);
                if (product is null)
                {
                    return Results.BadRequest(new { message = $"Produto '{item.ProductId}' nao foi encontrado." });
                }

                order.AddItemSnapshot(new OrderItemSnapshot(
                    product.Id,
                    item.Quantity,
                    product.Price,
                    "BRL",
                    product.Name,
                    product.Slug,
                    product.CategorySlug));

                order.AddDeliveryInstruction(DeliveryInstruction.CreateManual(
                    product.Id,
                    $"Pedido pendente via WhatsApp. Servidor: {item.Server ?? "nao informado"}",
                    request.Discord ?? request.Email));
            }

            await checkoutRepository.SaveOrderAsync(order, ct);

            return Results.Ok(new SupportPendingCheckoutResponseDto(
                order.Id,
                order.OrderIntentKey,
                order.Status.ToString(),
                GetStatusLabel(order.Status)));
        })
        .WithTags("Public Checkout");

        var cartGroup = app.MapGroup("/cart")
            .RequireAuthorization(AuthPolicies.VerifiedForSensitiveActions);

        cartGroup.MapGet("", async (ClaimsPrincipal user, CartService cartService, CancellationToken ct) =>
        {
            var customerId = ResolveCustomerId(user);
            var cart = await cartService.GetCartAsync(new GetCartRequest(customerId), ct);
            return Results.Ok(ToCartDto(cart));
        });

        cartGroup.MapPost("/items", async (ClaimsPrincipal user, AddCartItemDto request, CartService cartService, CancellationToken ct) =>
        {
            var customerId = ResolveCustomerId(user);
            var cart = await cartService.AddItemAsync(new AddCartItemRequest(customerId, request.ProductId, request.Quantity), ct);
            return Results.Ok(ToCartDto(cart));
        });

        cartGroup.MapPut("/items/{productId:guid}", async (ClaimsPrincipal user, Guid productId, SetCartItemQuantityDto request, CartService cartService, CancellationToken ct) =>
        {
            var customerId = ResolveCustomerId(user);
            var cart = await cartService.SetItemQuantityAsync(new SetCartItemQuantityRequest(customerId, productId, request.Quantity), ct);
            return Results.Ok(ToCartDto(cart));
        });

        cartGroup.MapDelete("/items/{productId:guid}", async (ClaimsPrincipal user, Guid productId, CartService cartService, CancellationToken ct) =>
        {
            var customerId = ResolveCustomerId(user);
            var cart = await cartService.RemoveItemAsync(new RemoveCartItemRequest(customerId, productId), ct);
            return Results.Ok(ToCartDto(cart));
        });

        var ordersGroup = app.MapGroup("/orders")
            .RequireAuthorization(AuthPolicies.VerifiedForSensitiveActions);

        ordersGroup.MapPost("/submit", async (ClaimsPrincipal user, SubmitCheckoutDto request, CheckoutService checkoutService, CancellationToken ct) =>
        {
            var customerId = ResolveCustomerId(user);
            var response = await checkoutService.SubmitCheckoutAsync(
                new SubmitCheckoutRequest(
                    customerId,
                    request.DeliveryInstructions.Select(x => new CheckoutDeliveryInstructionRequest(
                        x.ProductId,
                        x.TargetCharacter,
                        x.TargetServer,
                        x.DeliveryChannelOrContact,
                        x.RequestBrief,
                        x.ContactHandle)).ToList()),
                ct);

            return Results.Ok(ToSubmitResponseDto(response));
        });

        ordersGroup.MapGet("/{orderId:guid}", async (ClaimsPrincipal user, Guid orderId, ICheckoutRepository checkoutRepository, CancellationToken ct) =>
        {
            var customerId = ResolveCustomerId(user);
            var order = await checkoutRepository.GetOrderByIdAsync(orderId, ct);
            if (order is null || order.CustomerId != customerId)
            {
                return Results.NotFound();
            }

            // Per D-11: Include statusCode and statusLabel
            return Results.Ok(new OrderResponseDto(
                order.Id,
                order.CustomerId,
                order.OrderIntentKey,
                order.CreatedAtUtc,
                order.Status.ToString(),
                GetStatusLabel(order.Status),
                order.Items.Select(x => new CheckoutOrderItemDto(
                    x.ProductId,
                    x.Quantity,
                    x.UnitPrice,
                    x.Currency,
                    x.ProductName,
                    x.ProductSlug,
                    x.CategorySlug)).ToList(),
                order.DeliveryInstructions.Select(x => new CheckoutDeliveryInstructionResponseDto(
                    x.ProductId,
                    x.FulfillmentType.ToString(),
                    x.Status.ToString(),
                    x.CompletedAtUtc,
                    x.TargetCharacter,
                    x.TargetServer,
                    x.DeliveryChannelOrContact,
                    x.RequestBrief,
                    x.ContactHandle)).ToList()));
        });

        ordersGroup.MapPost("/{orderId:guid}/payments/preference", async (
            ClaimsPrincipal user,
            Guid orderId,
            PaymentPreferenceService paymentPreferenceService,
            CancellationToken ct) =>
        {
            var customerId = ResolveCustomerId(user);
            try
            {
                var preference = await paymentPreferenceService.CreatePreferenceAsync(orderId, customerId, ct);
                return Results.Ok(new CreatePaymentPreferenceResponseDto(
                    preference.PreferenceId,
                    preference.InitPointUrl,
                    preference.ExternalReference));
            }
            catch (PaymentPreferenceOrderNotFoundException)
            {
                return Results.NotFound();
            }
        });

        // Per D-09, D-12: Customer order history list with pagination
        ordersGroup.MapGet("", async (ClaimsPrincipal user, int page, int pageSize, IOrderLifecycleRepository repository, CancellationToken ct) =>
        {
            var customerId = ResolveCustomerId(user);
            pageSize = Math.Clamp(pageSize, 1, 50);
            
            var orders = await repository.GetCustomerOrdersAsync(customerId, page, pageSize, ct);
            
            var items = orders.Select(o => new OrderListItemDto(
                o.Id,
                o.OrderIntentKey,
                o.CreatedAtUtc,
                o.Status.ToString(),
                GetStatusLabel(o.Status),
                o.CustomerName,
                o.CustomerEmail,
                o.CustomerDiscord,
                o.PaymentMethod,
                o.Items.Sum(item => item.UnitPrice * item.Quantity),
                o.Items.Sum(item => item.Quantity))).ToList();
                
            return Results.Ok(new PaginatedOrderListDto(items, page, pageSize, items.Count));
        });

        return app;
    }

    private static Guid ResolveCustomerId(ClaimsPrincipal user)
    {
        var subject = user.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var customerId) || customerId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated subject claim is missing or invalid.", "sub");
        }

        return customerId;
    }

    // Per D-11: Get display label for status
    private static string GetStatusLabel(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Pendente",
        OrderStatus.Paid => "Pago",
        OrderStatus.Cancelled => "Cancelado",
        _ => status.ToString()
    };

    private static CartResponseDto ToCartDto(CartResponse response)
    {
        return new CartResponseDto(
            response.CustomerId,
            response.Lines.Select(x => new CartLineDto(x.ProductId, x.Quantity)).ToList());
    }

    private static SubmitCheckoutResponseDto ToSubmitResponseDto(SubmitCheckoutResponse response)
    {
        return new SubmitCheckoutResponseDto(
            response.OrderId,
            response.OrderIntentKey,
            response.Items.Select(x => new CheckoutOrderItemDto(
                x.ProductId,
                x.Quantity,
                x.UnitPrice,
                x.Currency,
                x.ProductName,
                x.ProductSlug,
                x.CategorySlug)).ToList(),
            response.DeliveryInstructions.Select(x => new CheckoutDeliveryInstructionResponseDto(
                x.ProductId,
                x.FulfillmentType.ToString(),
                x.Status.ToString(),
                x.CompletedAtUtc,
                x.TargetCharacter,
                x.TargetServer,
                x.DeliveryChannelOrContact,
                x.RequestBrief,
                x.ContactHandle)).ToList());
    }
}
