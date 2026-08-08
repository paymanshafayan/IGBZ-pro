namespace IGBZ.Infrastructure;

using IGBZ.Application.Abstractions;
using IGBZ.Application.Auth;
using IGBZ.Application.BNPL;
using IGBZ.Application.Discounts;
using IGBZ.Application.Payments;
using IGBZ.Application.Tenancy;
using IGBZ.Infrastructure.Auth;
using IGBZ.Infrastructure.Gateways;
using IGBZ.Infrastructure.Mongo;
using IGBZ.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

/// <summary>ثبت سرویس‌های زیرساخت در DI.</summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddIgBzInfrastructure(this IServiceCollection services, MongoOptions mongoOptions)
        => services.AddIgBzInfrastructure(mongoOptions, jwtOptions: null);

    public static IServiceCollection AddIgBzInfrastructure(this IServiceCollection services, MongoOptions mongoOptions, JwtOptions? jwtOptions)
    {
        services.AddSingleton(mongoOptions);
        services.AddSingleton<MongoDbContext>();

        services.AddScoped<ITenantContext, TenantContext>();

        // JWT — اگر تنظیمات داده شده باشد
        if (jwtOptions != null)
        {
            services.AddSingleton(jwtOptions);
            services.AddScoped<IJwtTokenService, JwtTokenService>();
        }

        // ── درگاه‌های پرداخت ──
        services.AddScoped<IPaymentGateway, TestPaymentGateway>();      // توسعه/تست
        services.AddScoped<IPaymentGateway>(sp =>
            new PayIrGateway(sp.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient("PayIr")));
        services.AddScoped<IPaymentGateway>(sp =>
            new NowPaymentsGateway(sp.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient("NowPayments")));

        // ── درگاه‌های BNPL ──
        services.AddScoped<IBnplGateway>(sp =>
            new DigipayBnplGateway(sp.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient("Digipay")));
        services.AddScoped<IBnplGateway>(sp =>
            new SnapppayBnplGateway(sp.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient("Snapppay")));

        // HttpClientهای نام‌گذاری‌شده برای درگاه‌ها
        services.AddHttpClient("PayIr");
        services.AddHttpClient("NowPayments");
        services.AddHttpClient("Digipay");
        services.AddHttpClient("Snapppay");

        // Repository های عمومی (باز) + تننت‌محور
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped(typeof(ITenantScopedRepository<>), typeof(TenantScopedRepository<>));

        // عملیات اتمیک موجودی
        services.AddScoped<IProductInventoryRepository, ProductInventoryRepository>();

        // دسترسی کوپن‌ها برای موتور تخفیف
        services.AddScoped<IDiscountLookup, MongoDiscountLookup>();

        return services;
    }
}
