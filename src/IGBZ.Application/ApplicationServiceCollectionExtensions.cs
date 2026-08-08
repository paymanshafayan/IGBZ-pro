namespace IGBZ.Application;

using IGBZ.Application.Auth;
using IGBZ.Application.Discounts;
using IGBZ.Application.Orders;
using IGBZ.Application.Payments;
using IGBZ.Application.Pricing;
using IGBZ.Application.Provisioning;
using IGBZ.Application.Subscriptions;
using IGBZ.Application.Wallets;
using Microsoft.Extensions.DependencyInjection;

/// <summary>ثبت سرویس‌های لایهٔ اپلیکیشن در DI.</summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddIgBzApplication(this IServiceCollection services)
    {
        // قیمت‌گذاری — ترتیب calculators مهم است (SubTotal ← Discount ← Tax ← Shipping)
        services.AddScoped<IPricingCalculator, SubtotalCalculator>();
        services.AddScoped<IPricingCalculator, DiscountCalculator>();
        services.AddScoped<IPricingCalculator, TaxCalculator>();
        services.AddScoped<IPricingCalculator, ShippingCalculator>();
        services.AddScoped<IPricingPipeline, PricingPipeline>();

        // تخفیف
        services.AddScoped<IDiscountEngine, DiscountEngine>();

        // کیف‌پول
        services.AddScoped<IWalletService, WalletService>();

        // سفارش
        services.AddScoped<IOrderService, OrderService>();

        // پرداخت (درگاه‌های واقعی در فاز ۶ ثبت می‌شوند)
        services.AddScoped<IPaymentService, PaymentService>();

        // احراز هویت
        services.AddScoped<IAuthService, AuthService>();

        // Provisioning + اشتراک
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
        services.AddScoped<ITenantSubscriptionService, TenantSubscriptionService>();

        return services;
    }
}
