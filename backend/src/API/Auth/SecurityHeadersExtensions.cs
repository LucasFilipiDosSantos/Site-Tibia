using Microsoft.Extensions.Primitives;

namespace API.Auth;

public static class SecurityHeadersExtensions
{
    private static readonly PathString[] SwaggerPrefixes =
    [
        new("/swagger"),
        new("/api/swagger"),
        new("/openapi"),
        new("/api/openapi")
    ];

    public static IApplicationBuilder UseSecurityHeaders(
        this IApplicationBuilder app,
        IReadOnlyCollection<string> allowedFrontendOrigins)
    {
        var environment = app.ApplicationServices.GetRequiredService<IHostEnvironment>();
        var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
        var connectSrc = BuildConnectSources(configuration, environment, allowedFrontendOrigins);

        return app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;

                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Permissions-Policy"] =
                    "accelerometer=(), autoplay=(), camera=(), display-capture=(), encrypted-media=(), fullscreen=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";

                headers["Content-Security-Policy"] = IsSwaggerRequest(context.Request.Path)
                    ? BuildSwaggerContentSecurityPolicy(connectSrc)
                    : BuildDefaultContentSecurityPolicy(connectSrc, environment);

                ApplyCacheHeaders(context.Request.Path, headers);
                return Task.CompletedTask;
            });

            await next();
        });
    }

    private static string BuildDefaultContentSecurityPolicy(
        IReadOnlyCollection<string> connectSrc,
        IHostEnvironment environment)
    {
        var directives = new List<string>
        {
            "default-src 'self'",
            "script-src 'self'",
            "style-src 'self'",
            "img-src 'self' data: https:",
            "font-src 'self' data:",
            $"connect-src {string.Join(' ', connectSrc)}",
            "frame-ancestors 'none'",
            "base-uri 'self'",
            "form-action 'self'",
            "object-src 'none'"
        };

        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            directives.Add("upgrade-insecure-requests");
        }

        return string.Join("; ", directives) + ";";
    }

    private static string BuildSwaggerContentSecurityPolicy(IReadOnlyCollection<string> connectSrc)
    {
        var directives = new[]
        {
            "default-src 'self'",
            "script-src 'self' 'unsafe-inline'",
            "style-src 'self' 'unsafe-inline'",
            "img-src 'self' data:",
            "font-src 'self' data:",
            $"connect-src {string.Join(' ', connectSrc)}",
            "frame-ancestors 'none'",
            "base-uri 'self'",
            "form-action 'self'",
            "object-src 'none'"
        };

        return string.Join("; ", directives) + ";";
    }

    private static string[] BuildConnectSources(
        IConfiguration configuration,
        IHostEnvironment environment,
        IReadOnlyCollection<string> allowedFrontendOrigins)
    {
        var configuredSources = configuration.GetSection("SecurityHeaders:Csp:ConnectSrc").Get<string[]>()
            ?? [];

        var developmentSources = environment.IsDevelopment()
            ?
            [
                "http://localhost:8080",
                "http://127.0.0.1:8080",
                "http://localhost:5071",
                "https://localhost:7145",
                "ws://localhost:5173",
                "ws://127.0.0.1:5173"
            ]
            : Array.Empty<string>();

        return new[] { "'self'" }
            .Concat(allowedFrontendOrigins)
            .Concat(developmentSources)
            .Concat(configuredSources)
            .Select(NormalizeSource)
            .Where(source => !string.IsNullOrWhiteSpace(source) && source != "*")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string NormalizeSource(string source)
    {
        source = source.Trim().TrimEnd('/');

        if (source is "'self'" or "data:" or "https:" or "http:" or "ws:" or "wss:")
        {
            return source;
        }

        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri))
        {
            return string.Empty;
        }

        if (uri.Scheme is not ("http" or "https" or "ws" or "wss"))
        {
            return string.Empty;
        }

        return uri.IsDefaultPort
            ? $"{uri.Scheme}://{uri.Host}"
            : $"{uri.Scheme}://{uri.Host}:{uri.Port}";
    }

    private static bool IsSwaggerRequest(PathString path)
    {
        return SwaggerPrefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static void ApplyCacheHeaders(PathString path, IHeaderDictionary headers)
    {
        if (IsSwaggerRequest(path))
        {
            headers["Cache-Control"] = "no-cache, no-store, max-age=0, must-revalidate";
        }
        else
        {
            headers["Cache-Control"] = "no-store, max-age=0";
        }

        headers["Pragma"] = "no-cache";
        headers["Expires"] = "0";
        headers["Vary"] = StringValues.Concat(headers.Vary, new StringValues("Origin"));
    }
}
