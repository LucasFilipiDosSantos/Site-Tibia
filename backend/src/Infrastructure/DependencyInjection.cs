using Application.Catalog.Contracts;
using Application.Checkout.Contracts;
using Application.Identity.Contracts;
using Application.Inventory.Contracts;
using Domain.Identity;
using Infrastructure.Catalog.Repositories;
using Infrastructure.Checkout;
using Infrastructure.Checkout.Repositories;
using Infrastructure.Inventory.Repositories;
using Infrastructure.Identity.Repositories;
using Infrastructure.Identity.Options;
using Infrastructure.Identity.Services;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=tibia_webstore;Username=postgres;Password=postgres";

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.MapEnum<UserRole>("user_role")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshSessionRepository, RefreshSessionRepository>();
        services.AddScoped<ISecurityTokenRepository, SecurityTokenRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<ICheckoutRepository, CheckoutRepository>();
        services.AddScoped<ICartProductAvailabilityGateway, CartProductAvailabilityGateway>();
        services.AddScoped<ICheckoutProductCatalogGateway, CheckoutProductCatalogGateway>();
        services.AddScoped<ICheckoutInventoryGateway, CheckoutInventoryGateway>();
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddSingleton<ISystemClock, SystemClock>();
        services
            .AddOptions<IdentityTokenDeliveryOptions>()
            .Bind(configuration.GetSection(IdentityTokenDeliveryOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<IdentityTokenDeliveryOptions>, IdentityTokenDeliveryOptionsValidator>();

        services.AddScoped<InMemoryIdentityTokenDelivery>();
        services.AddScoped<SmtpIdentityTokenDelivery>();
        services.AddSingleton<ISmtpTokenTransport, SmtpClientTokenTransport>();

        services.AddScoped<IIdentityTokenDelivery>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<IdentityTokenDeliveryOptions>>().Value;
            if (string.Equals(options.Provider, "smtp", StringComparison.OrdinalIgnoreCase))
            {
                return sp.GetRequiredService<SmtpIdentityTokenDelivery>();
            }

            if (string.Equals(options.Provider, "inmemory", StringComparison.OrdinalIgnoreCase))
            {
                return sp.GetRequiredService<InMemoryIdentityTokenDelivery>();
            }

            throw new InvalidOperationException(
                $"Unsupported IdentityTokenDelivery provider '{options.Provider}'. Supported values: smtp, inmemory.");
        });
        services.AddScoped<ITokenService>(sp =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            return new JwtTokenService(new JwtTokenServiceOptions
            {
                Issuer = cfg["Jwt:Issuer"] ?? "tibia-webstore",
                Audience = cfg["Jwt:Audience"] ?? "tibia-webstore-client",
                SigningKey = cfg["Jwt:SigningKey"] ?? "01234567890123456789012345678901"
            });
        });

        return services;
    }
}
