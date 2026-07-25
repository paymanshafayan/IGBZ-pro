using IGBZ.Application.Abstractions;
using IGBZ.Application.Auth;
using IGBZ.Application.Catalog;
using IGBZ.Application.Common;
using IGBZ.Application.Integrations;
using IGBZ.Application.Inventory;
using IGBZ.Application.Orders;
using IGBZ.Application.Payments;
using IGBZ.Application.Pricing;
using IGBZ.Application.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace IGBZ.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIgbzApplication(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IOrderNumberGenerator, UtcSequentialOrderNumberGenerator>();
        services.AddSingleton<CommercePricingOptions>();
        services.AddSingleton<OtpOptions>();
        services.AddScoped<IOrderTotalCalculator, OrderTotalCalculator>();
        services.AddScoped<DiscountService>();
        services.AddScoped<TenantProvisioningService>();
        services.AddScoped<CatalogService>();
        services.AddScoped<OrderService>();
        services.AddScoped<PaymentApplicationService>();
        services.AddScoped<IntegrationService>();
        services.AddScoped<InventoryApplicationService>();
        services.AddScoped<AuthService>();
        return services;
    }
}
