namespace IGBZ.Application;

using IGBZ.Application.Accounting;
using IGBZ.Application.Admin;
using IGBZ.Application.AiStudio;
using IGBZ.Application.Auth;
using IGBZ.Application.BNPL;
using IGBZ.Application.Catalog;
using IGBZ.Application.Discounts;
using IGBZ.Application.Lms;
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

        // تخفیف (سطح ۲ کوپن + سطح ۳ قواعد)
        services.AddScoped<IDiscountRule, IGBZ.Domain.Discounts.PercentageAboveThresholdRule>(sp =>
            new IGBZ.Domain.Discounts.PercentageAboveThresholdRule(thresholdToman: 1_000_000, percent: 5, priority: 10, combinable: true));
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

        // استودیوی AI محتوا
        services.AddScoped<IAiStudioService, AiStudioService>();
        services.AddScoped<IBackgroundMusicCatalogService, BackgroundMusicCatalogService>();

        // LMS (امنیت ویدیو در لایهٔ زیرساخت ثبت می‌شود)
        services.AddScoped<ICourseService, CourseService>();

        // حسابداری (فاکتور رسمی + مؤدیان)
        services.AddScoped<IAccountingService, AccountingService>();

        return services;
    }
}
