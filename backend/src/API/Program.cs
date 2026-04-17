using System.Text;
using API.Auth;
using API.Catalog;
using API.Checkout;
using API.ErrorHandling;
using API.Inventory;
using Application.Catalog.Services;
using Application.Checkout.Services;
using Application.Identity.Contracts;
using Application.Identity.Services;
using Application.Inventory.Services;
using Application.Payments.Services;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<AuthRateLimitMiddleware>();

        app.MapAuthEndpoints();
        app.MapCatalogEndpoints();
        app.MapInventoryEndpoints();
        app.MapCheckoutEndpoints();

        app.Run();
    }
}
