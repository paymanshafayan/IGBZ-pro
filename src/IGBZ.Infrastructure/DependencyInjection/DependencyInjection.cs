using IGBZ.Application.Abstractions;
using IGBZ.Infrastructure.Auth;
using IGBZ.Infrastructure.Integrations;
using IGBZ.Infrastructure.Inventory;
using IGBZ.Infrastructure.MongoDb;
using IGBZ.Infrastructure.Payments;
using IGBZ.Infrastructure.Repositories;
using IGBZ.Infrastructure.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IGBZ.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIgbzInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoDbOptions>(configuration.GetSection(MongoDbOptions.SectionName));
        services.Configure<PlatformOptions>(configuration.GetSection(PlatformOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();
        services.AddSingleton<IPlatformDomainProvider, OptionsPlatformDomainProvider>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<MongoDbContext>();
        services.AddScoped(typeof(ITenantScopedRepository<>), typeof(TenantScopedMongoRepository<>));
        services.AddScoped(typeof(IPlatformRepository<>), typeof(PlatformMongoRepository<>));
        services.AddScoped<IInventoryService, MongoInventoryService>();
        services.AddScoped<IPaymentGatewayService, MockPaymentGatewayService>();
        services.AddHostedService<MongoIndexInitializer>();
        services.AddHostedService<MongoIntegrationJobWorker>();

        return services;
    }
}
