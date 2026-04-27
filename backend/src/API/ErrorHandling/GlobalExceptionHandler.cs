using Application.Identity.Exceptions;
using Application.Catalog.Contracts;
using Application.Checkout.Contracts;
using Application.Inventory.Contracts;
using Application.Payments.Contracts;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace API.ErrorHandling;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService
) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        if (exception is TokenDeliveryUnavailableException)
        {
            logger.LogWarning(
                exception,
                "Token delivery unavailable while processing {Path}; returning generic success contract.",
                httpContext.Request.Path
            );

            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.Response.ContentType = "application/json";

            var message = httpContext.Request.Path.Value switch
            {
                "/auth/verify-email/request" => "If the account exists, a verification link was sent.",
                "/auth/password-reset/request" => "If the account exists, a reset link was sent.",
                _ => "Request accepted.",
            };

            await httpContext.Response.WriteAsJsonAsync(new { message }, cancellationToken);
            return true;
        }

        var (status, title, detail) = Map(exception);

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception while processing {Path}", httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(exception, "Handled exception while processing {Path}", httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{status}",
            Instance = httpContext.Request.Path,
        };

        if (exception is InventoryReservationConflictException inventoryConflict)
        {
            problem.Extensions["availableQuantity"] = inventoryConflict.AvailableQuantity;
        }

        if (exception is CartStockConflictException cartConflict)
        {
            problem.Extensions["lineConflicts"] = new[]
            {
                new
                {
                    productId = cartConflict.ProductId,
                    requestedQuantity = cartConflict.RequestedQuantity,
                    availableQuantity = cartConflict.AvailableQuantity
                }
            };
        }

        if (exception is CheckoutReservationConflictException checkoutConflict)
        {
            problem.Extensions["lineConflicts"] = checkoutConflict.LineConflicts.Select(x => new
            {
                productId = x.ProductId,
                requestedQuantity = x.RequestedQuantity,
                availableQuantity = x.AvailableQuantity
            }).ToArray();
        }

        // Per D-15: ForbiddenStatusTransitionException includes currentStatus and allowedTransitions
        if (exception is ForbiddenStatusTransitionException transitionException)
        {
            problem.Extensions["currentStatus"] = transitionException.CurrentStatus.ToString();
            problem.Extensions["allowedTransitions"] = transitionException.AllowedTransitions.Select(t => t.ToString()).ToList();
        }

        await problemDetailsService.WriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problem,
                Exception = exception,
            }
        );

        return true;
    }

    private static (int status, string title, string detail) Map(Exception exception)
    {
        return exception switch
        {
            ArgumentException argumentException => (
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                argumentException.ParamName is null
                    ? argumentException.Message
                    : argumentException.Message.Replace(
                        $" (Parameter '{argumentException.ParamName}')",
                        string.Empty,
                        StringComparison.Ordinal
                    )
            ),
            UnauthorizedAccessException unauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized.",
                unauthorizedAccessException.Message
            ),
            InvalidOperationException invalidOperationException
                when invalidOperationException.Message.Contains(
                    "already exists",
                    StringComparison.OrdinalIgnoreCase
                ) => (
                    StatusCodes.Status409Conflict,
                    "Conflict.",
                    invalidOperationException.Message
                ),
            InventoryReservationConflictException inventoryConflictException => (
                StatusCodes.Status409Conflict,
                "Conflict.",
                inventoryConflictException.Message
            ),
            DuplicateProductReviewException duplicateProductReviewException => (
                StatusCodes.Status409Conflict,
                "Conflict.",
                duplicateProductReviewException.Message
            ),
            CartStockConflictException cartStockConflictException => (
                StatusCodes.Status409Conflict,
                "Conflict.",
                cartStockConflictException.Message
            ),
            CheckoutReservationConflictException checkoutConflictException => (
                StatusCodes.Status409Conflict,
                "Conflict.",
                checkoutConflictException.Message
            ),
            PaymentPreferenceProviderException paymentProviderException => (
                StatusCodes.Status502BadGateway,
                "Payment provider unavailable.",
                paymentProviderException.Message
            ),
            // Per D-15: Lifecycle conflict returns 409
            ForbiddenStatusTransitionException => (
                StatusCodes.Status409Conflict,
                "Status Transition Conflict.",
                "The requested status transition is not allowed from the current order status."
            ),
            ProductReviewPurchaseRequiredException productReviewPurchaseRequiredException => (
                StatusCodes.Status403Forbidden,
                "Forbidden.",
                productReviewPurchaseRequiredException.Message
            ),
            InvalidOperationException invalidOperationException => (
                StatusCodes.Status400BadRequest,
                "Operation failed.",
                invalidOperationException.Message
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Server error.",
                "An unexpected error occurred."
            ),
        };
    }
}
