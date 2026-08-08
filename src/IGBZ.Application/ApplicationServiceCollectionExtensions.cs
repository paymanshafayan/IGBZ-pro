namespace IGBZ.Application;

using IGBZ.Application.Admin;
using IGBZ.Application.Auth;
using IGBZ.Application.BNPL;
using IGBZ.Application.Catalog;
using IGBZ.Application.Discounts;
using IGBZ.Application.Logistics;
using IGBZ.Application.Marketplace;
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

        // کاتالوگ عمومی (Storefront)
        services.AddScoped<ICatalogService, CatalogService>();

        // ادمین تننت (محصولات + داشبورد)
        services.AddScoped<IAdminProductService, AdminProductService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();

        // BNPL
        services.AddScoped<IBnplService, BnplService>();

        // مارکت‌پلیس + لجستیک
        services.AddScoped<IMarketplaceService, MarketplaceService>();
        services.AddScoped<ILogisticsService, LogisticsService>();

        return services;
    }
}
