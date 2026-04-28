using Application.Audit.Contracts;
using Application.Audit.Services;
using Infrastructure.Audit;
using Application.Catalog.Contracts;
using Application.Checkout;
using Application.Checkout.Contracts;
using Application.Checkout.Services;
using Application.Identity.Contracts;
using Application.Inventory.Contracts;
using Application.Notifications;
using Application.Payments.Contracts;
using Application.Payments.Services;
using Domain.Identity;
using Infrastructure.Catalog.Repositories;
using Infrastructure.Checkout;
using Infrastructure.Checkout.Repositories;
using Infrastructure.Inventory.Repositories;
using Infrastructure.Identity.Repositories;
using Infrastructure.Identity.Options;
using Infrastructure.Identity.Services;
using Infrastructure.Jobs;
using Infrastructure.Notifications;
using Infrastructure.Persistence;
using Infrastructure.Payments.MercadoPago;
using Infrastructure.Payments.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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

        services.AddHangfireServices(configuration);
        services.AddDbContextPool<AppDbContext>(options =>
            options
                .UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MapEnum<UserRole>("user_role");
                    npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                    npgsql.CommandTimeout(15);
                })
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshSessionRepository, RefreshSessionRepository>();
        services.AddScoped<ISecurityTokenRepository, SecurityTokenRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductReviewRepository, ProductReviewRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<ICheckoutRepository, CheckoutRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IOrderLifecycleRepository, OrderLifecycleRepository>();
        services.AddScoped<ICartProductAvailabilityGateway, CartProductAvailabilityGateway>();
        services.AddScoped<ICheckoutProductCatalogGateway, CheckoutProductCatalogGateway>();
        services.AddScoped<ICheckoutInventoryGateway, CheckoutInventoryGateway>();
        services.AddDbContextPool<NotificationDbContext>(options =>
            options
                .UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MapEnum<UserRole>("user_role");
                    npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                    npgsql.CommandTimeout(15);
                })
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));
        services
            .AddOptions<MercadoPagoOptions>()
            .Bind(configuration.GetSection(MercadoPagoOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<MercadoPagoOptions>, MercadoPagoOptionsValidator>();
        services.AddScoped<IMercadoPagoPreferenceGateway>(sp =>
            new MercadoPagoPreferenceGateway(sp.GetRequiredService<IOptions<MercadoPagoOptions>>().Value));
        services.AddScoped<IPaymentWebhookSignatureValidator, MercadoPagoWebhookSignatureValidator>();
        services.AddScoped<PaymentWebhookIngressService>();
        services.AddScoped<IPaymentWebhookProcessor, PaymentWebhookProcessor>();
        services.AddScoped<IPaymentWebhookLogRepository, PaymentWebhookLogRepository>();
        services.AddScoped<IPaymentStatusEventRepository, PaymentStatusEventRepository>();
        services.AddScoped<IPaymentEventDedupRepository, PaymentEventDedupRepository>();
        services.AddScoped<IPaymentLinkRepository, PaymentLinkRepository>();
        services.AddScoped<INotificationOutboxRepository, NotificationOutboxRepository>();
        services.AddScoped<INotificationPublisher, NotificationPublisher>();
        services.AddScoped<OrderLifecycleService>();
        services.AddScoped<IFulfillmentService, FulfillmentService>();
        services.AddScoped<IAdminFulfillmentService, AdminFulfillmentService>();
        services.AddScoped<ICustomOrderRepository, CustomOrderRepository>();
        services.AddScoped<ICustomOrderService, CustomOrderService>();
        services.AddScoped<PaymentConfirmationService>();
        services.AddScoped<LegacyOrderCustomerLinkRepairService>();
        services.AddScoped<PaymentPreferenceSettings>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MercadoPagoOptions>>().Value;
            return new PaymentPreferenceSettings(
                options.NotificationUrl,
                options.SuccessUrl,
                options.FailureUrl,
                options.PendingUrl);
        });
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

        services.AddOptions<WhatsAppOptions>()
            .Bind(configuration.GetSection(WhatsAppOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<WhatsAppOptions>, WhatsAppOptionsValidator>();
        services.AddHttpClient<IWhatsAppNotificationService, WhatsAppNotificationService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // Audit services
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        return services;
    }
}
