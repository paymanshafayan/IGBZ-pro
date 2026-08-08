namespace IGBZ.Infrastructure;

using IGBZ.Application.Abstractions;
using IGBZ.Application.Discounts;
using IGBZ.Application.Tenancy;
using IGBZ.Infrastructure.Mongo;
using IGBZ.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

/// <summary>ثبت سرویس‌های زیرساخت در DI.</summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddIgBzInfrastructure(this IServiceCollection services, MongoOptions mongoOptions)
    {
        services.AddSingleton(mongoOptions);
        services.AddSingleton<MongoDbContext>();

        services.AddScoped<ITenantContext, TenantContext>();

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
