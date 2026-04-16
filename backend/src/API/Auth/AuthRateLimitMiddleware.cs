using System.Collections.Concurrent;

namespace API.Auth;

public sealed class AuthRateLimitMiddleware
{
    private static readonly ConcurrentDictionary<string, SlidingWindowCounter> Counters = new();
    private readonly RequestDelegate _next;

    public AuthRateLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var isSensitiveAuthPath = path.EndsWith("/auth/login") || path.EndsWith("/auth/password-reset/request");
        if (!isSensitiveAuthPath)
        {
            await _next(context);
            return;
        }

        var userKey = context.Request.Headers["X-Auth-Identifier"].FirstOrDefault() ?? "anonymous";
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var compositeKey = $"{path}:{userKey.ToUpperInvariant()}:{ip}";

        var counter = Counters.GetOrAdd(compositeKey, _ => new SlidingWindowCounter());
        if (!counter.TryConsume(DateTimeOffset.UtcNow, 5, TimeSpan.FromMinutes(1)))
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new { message = "Too many attempts. Try again later." });
            return;
        }

        await _next(context);
    }

    private sealed class SlidingWindowCounter
    {
        private readonly Queue<DateTimeOffset> _attempts = new();
        private readonly object _lock = new();

        public bool TryConsume(DateTimeOffset nowUtc, int limit, TimeSpan window)
        {
            lock (_lock)
            {
                while (_attempts.Count > 0 && nowUtc - _attempts.Peek() > window)
                {
                    _attempts.Dequeue();
                }

                if (_attempts.Count >= limit)
                {
                    return false;
                }

                _attempts.Enqueue(nowUtc);
                return true;
            }
        }
    }
}
