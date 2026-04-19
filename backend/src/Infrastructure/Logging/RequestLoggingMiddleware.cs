using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Logging;

/// <summary>
/// Request logging middleware that extracts or generates correlation ID and ensures it flows through the entire request chain.
/// D-14: Correlation spans full chain: Payment -> Order -> Fulfillment -> Notification.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public const string CorrelationIdHeader = "X-Correlation-ID";
    public const string CorrelationIdItemKey = "CorrelationId";

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        context.Items[CorrelationIdItemKey] = correlationId;

        // Add correlation ID to response header
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        _logger.LogDebug(
            "Request {Method} {Path} with correlation ID {CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            correlationId);

        try
        {
            await _next(context);
        }
        finally
        {
            _logger.LogDebug(
                "Request {Method} {Path} completed with correlation ID {CorrelationId}, status {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                correlationId,
                context.Response.StatusCode);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        // Try to extract from incoming request header
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var existingCorrelationId) &&
            !string.IsNullOrWhiteSpace(existingCorrelationId))
        {
            return existingCorrelationId.ToString();
        }

        // Generate new correlation ID
        return Guid.NewGuid().ToString("N");
    }
}

/// <summary>
/// Provides correlation ID access throughout the application via HttpContext.
/// </summary>
public static class CorrelationContext
{
    private const string CorrelationIdItemKey = RequestLoggingMiddleware.CorrelationIdItemKey;

    /// <summary>
    /// Gets the current correlation ID from HttpContext, or null if not available.
    /// </summary>
    public static string? GetCorrelationId(HttpContext context)
    {
        return context.Items[CorrelationIdItemKey] as string;
    }

    /// <summary>
    /// Sets the correlation ID in HttpContext items.
    /// </summary>
    public static void SetCorrelationId(HttpContext context, string correlationId)
    {
        context.Items[CorrelationIdItemKey] = correlationId;
    }
}