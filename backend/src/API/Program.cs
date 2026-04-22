using System.Text;
using System.IO.Compression;
using API.Admin;
using API.Auth;
using API.Catalog;
using API.Checkout;
using API.ErrorHandling;
using API.Inventory;
using API.Jobs;
using API.Payments;
using API.CustomOrders;
using Application.Catalog.Services;
using Application.Checkout.Services;
using Application.Identity.Contracts;
using Application.Identity.Services;
using Application.Inventory.Services;
using Application.Payments.Services;
using HealthChecks.Hangfire;
using Infrastructure;
using Infrastructure.Logging;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var renderPort = Environment.GetEnvironmentVariable("PORT");
        if (!string.IsNullOrWhiteSpace(renderPort))
        {
            builder.WebHost.UseUrls($"http://0.0.0.0:{renderPort}");
        }

        if (builder.Environment.IsEnvironment("Testing"))
        {
            builder.Logging.ClearProviders();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:Enabled"] = "false"
            });
        }

        // Add services to the container.
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddMemoryCache();
        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });
        builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });
        builder.Services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });
        });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        var frontendOrigins = GetFrontendOrigins(builder.Configuration, builder.Environment);
        var defaultFrontendOrigins = new[]
        {
            "http://localhost:5173",
            "http://127.0.0.1:5173",
            "http://localhost:4173",
            "http://127.0.0.1:4173",
            "http://localhost",
            "http://127.0.0.1"
        };
        var allowedFrontendOrigins = (builder.Environment.IsDevelopment() ? defaultFrontendOrigins : Array.Empty<string>())
            .Concat(frontendOrigins)
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim().TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
            {
                policy
                    .WithOrigins(allowedFrontendOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
        var healthChecks = builder.Services.AddHealthChecks();
        if (builder.Configuration.GetValue("Hangfire:Enabled", true))
        {
            healthChecks.AddHangfire(options =>
            {
                options.MaximumJobsFailed = 10;
                options.MinimumAvailableServers = 1;
            });
        }

        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddAuthPolicies();
        var jwtOptions =
            builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? new JwtOptions();
        if (
            string.IsNullOrWhiteSpace(jwtOptions.Issuer)
            || string.IsNullOrWhiteSpace(jwtOptions.Audience)
            || string.IsNullOrWhiteSpace(jwtOptions.SigningKey)
        )
        {
            throw new InvalidOperationException(
                "Jwt settings Issuer, Audience and SigningKey are required."
            );
        }

        if (jwtOptions.SigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt signing key must be at least 32 characters for HS256."
            );
        }

        if (!builder.Environment.IsDevelopment()
            && !builder.Environment.IsEnvironment("Testing")
            && string.Equals(jwtOptions.SigningKey, "01234567890123456789012345678901", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Jwt signing key is still using the development placeholder. Set Jwt:SigningKey from a production secret."
            );
        }

        builder
            .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtOptions.SigningKey)
                        ),
                        ValidateLifetime = true,
                        RoleClaimType = "role",
                        ClockSkew = TimeSpan.Zero,
                    };
                }
            );
        builder.Services.AddScoped<IIdentityService, IdentityService>();
        builder.Services.AddScoped<TokenRotationService>();
        builder.Services.AddSingleton<SecurityAuditService>();
        builder.Services.AddScoped<CatalogService>();
        builder.Services.AddScoped<InventoryService>();
        builder.Services.AddScoped<CartService>();
        builder.Services.AddScoped<CheckoutService>();
        builder.Services.AddScoped<PaymentPreferenceService>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (app.Environment.IsEnvironment("Testing"))
            {
                // Test hosts replace repositories/services and should not touch the real database.
            }
            else if (app.Environment.IsDevelopment())
            {
                try
                {
                    dbContext.Database.Migrate();
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("PendingModelChangesWarning", StringComparison.Ordinal))
                {
                    dbContext.Database.EnsureDeleted();
                    dbContext.Database.EnsureCreated();
                }

                DevelopmentDataSeeder.SeedAsync(dbContext).GetAwaiter().GetResult();
            }
            else
            {
                dbContext.Database.Migrate();
            }
        }

        // Configure the HTTP request pipeline.
        Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
        app.UseExceptionHandler();
        app.UseForwardedHeaders();
        app.UseResponseCompression();
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.RoutePrefix = "swagger";
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Tibia Webstore API v1");
            });
        }

        app.UseHttpsSecurity();
        app.UseCors("Frontend");
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<AuthRateLimitMiddleware>();

        var api = app.MapGroup("/api");
        api.MapAuthEndpoints();
        api.MapCatalogEndpoints();
        api.MapInventoryEndpoints();
        api.MapCheckoutEndpoints();
        api.MapAdminOrderEndpoints();
        var hangfireEnabled = app.Configuration.GetValue("Hangfire:Enabled", true);
        if (hangfireEnabled)
        {
            api.MapPaymentWebhookEndpoints();
        }

        api.MapCustomOrderEndpoints();
        api.MapAdminEndpoints();
        api.MapAdminAuditEndpoints();
        api.MapAdminWebhookLogEndpoints();
        api.MapGet("/healthz", () => Results.Ok(new
        {
            status = "ok",
            environment = app.Environment.EnvironmentName,
            checkedAtUtc = DateTimeOffset.UtcNow
        }))
        .AllowAnonymous()
        .WithName("Healthz")
        .WithTags("Health");
        api.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        }).AllowAnonymous();
        app.MapGet("/healthz", () => Results.Redirect("/api/healthz"))
            .AllowAnonymous()
            .ExcludeFromDescription();
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        }).AllowAnonymous();
        if (hangfireEnabled)
        {
            app.MapHangfireDashboard("/api/hangfire");
            api.MapNotificationJobEndpoints();
        }

        app.Run();
    }

    private static string[] GetFrontendOrigins(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? [];
        var frontendUrl = configuration["FRONTEND_URL"] ?? "https://lootera.com.br";
        var frontendBaseUrl = configuration["Frontend:BaseUrl"];
        var corsOrigins = configuration["CORS_ALLOWED_ORIGINS"];

        var appsettingsOrigins = environment.IsDevelopment()
            ? configuredOrigins.Concat(SplitOrigins(frontendBaseUrl))
            : [];

        return appsettingsOrigins
            .Concat(SplitOrigins(frontendUrl))
            .Concat(SplitOrigins(corsOrigins))
            .ToArray();
    }

    private static IEnumerable<string> SplitOrigins(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
