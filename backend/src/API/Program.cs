using System.Text;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("FrontendDev", policy =>
            {
                policy
                    .WithOrigins(
                        "http://localhost:5173",
                        "http://127.0.0.1:5173",
                        "http://localhost:4173",
                        "http://127.0.0.1:4173",
                        "http://localhost",
                        "http://127.0.0.1")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
        builder.Services.AddHealthChecks()
            .AddHangfire(options =>
            {
                options.MaximumJobsFailed = 10;
                options.MinimumAvailableServers = 1;
            });

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
            if (app.Environment.IsDevelopment())
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
        app.UseCors("FrontendDev");
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<AuthRateLimitMiddleware>();

        app.MapAuthEndpoints();
        app.MapCatalogEndpoints();
        app.MapInventoryEndpoints();
        app.MapCheckoutEndpoints();
        app.MapPaymentWebhookEndpoints();
        app.MapCustomOrderEndpoints();
        app.MapAdminEndpoints();
        app.MapAdminAuditEndpoints();
        app.MapAdminWebhookLogEndpoints();
        app.MapHangfireDashboard();
        app.MapNotificationJobEndpoints();

        app.Run();
    }
}
