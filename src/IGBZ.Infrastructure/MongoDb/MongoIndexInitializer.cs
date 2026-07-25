using IGBZ.Domain.Catalog;
using IGBZ.Domain.Discounts;
using IGBZ.Domain.Inventory;
using IGBZ.Domain.Identity;
using IGBZ.Domain.Integrations;
using IGBZ.Domain.Orders;
using IGBZ.Domain.Payments;
using IGBZ.Domain.Tenancy;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace IGBZ.Infrastructure.MongoDb;

public sealed class MongoIndexInitializer(MongoDbContext dbContext) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await CreateTenantIndexes(cancellationToken);
        await CreateIdentityIndexes(cancellationToken);
        await CreateCatalogIndexes(cancellationToken);
        await CreateInventoryIndexes(cancellationToken);
        await CreateOrderIndexes(cancellationToken);
        await CreatePaymentIndexes(cancellationToken);
        await CreateDiscountIndexes(cancellationToken);
        await CreateIntegrationIndexes(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;


    private async Task CreateIdentityIndexes(CancellationToken cancellationToken)
    {
        var users = dbContext.Collection<User>();
        await users.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.PhoneNumber),
                    new CreateIndexOptions { Unique = true }),
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.Email),
                    new CreateIndexOptions { Sparse = true })
            ],
            cancellationToken);

        var otp = dbContext.Collection<OtpChallenge>();
        await otp.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<OtpChallenge>(
                    Builders<OtpChallenge>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.PhoneNumber).Ascending(x => x.Purpose)),
                new CreateIndexModel<OtpChallenge>(
                    Builders<OtpChallenge>.IndexKeys.Ascending(x => x.ExpiresAtUtc))
            ],
            cancellationToken);
    }

    private async Task CreateTenantIndexes(CancellationToken cancellationToken)
    {
        var tenants = dbContext.Collection<Tenant>();
        await tenants.Indexes.CreateOneAsync(
            new CreateIndexModel<Tenant>(
                Builders<Tenant>.IndexKeys.Ascending(x => x.Subdomain),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        var plans = dbContext.Collection<TenantPlan>();
        await plans.Indexes.CreateOneAsync(
            new CreateIndexModel<TenantPlan>(
                Builders<TenantPlan>.IndexKeys.Ascending(x => x.Code),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        var domains = dbContext.Collection<StoreDomainMapping>();
        await domains.Indexes.CreateOneAsync(
            new CreateIndexModel<StoreDomainMapping>(
                Builders<StoreDomainMapping>.IndexKeys.Ascending(x => x.Host),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);
    }

    private async Task CreateCatalogIndexes(CancellationToken cancellationToken)
    {
        var products = dbContext.Collection<Product>();
        await products.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<Product>(
                    Builders<Product>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.Slug),
                    new CreateIndexOptions { Unique = true }),
                new CreateIndexModel<Product>(
                    Builders<Product>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.Status)),
                new CreateIndexModel<Product>(
                    Builders<Product>.IndexKeys.Ascending(x => x.TenantId).Ascending("variants.sku"))
            ],
            cancellationToken);

        var categories = dbContext.Collection<Category>();
        await categories.Indexes.CreateOneAsync(
            new CreateIndexModel<Category>(
                Builders<Category>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.Slug),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);
    }

    private async Task CreateInventoryIndexes(CancellationToken cancellationToken)
    {
        var inventory = dbContext.Collection<InventoryItem>();
        await inventory.Indexes.CreateOneAsync(
            new CreateIndexModel<InventoryItem>(
                Builders<InventoryItem>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.ProductId).Ascending(x => x.VariantId),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        var reservations = dbContext.Collection<StockReservation>();
        await reservations.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<StockReservation>(
                    Builders<StockReservation>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.Status)),
                new CreateIndexModel<StockReservation>(
                    Builders<StockReservation>.IndexKeys.Ascending(x => x.ExpiresAtUtc))
            ],
            cancellationToken);
    }

    private async Task CreateOrderIndexes(CancellationToken cancellationToken)
    {
        var orders = dbContext.Collection<Order>();
        await orders.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<Order>(
                    Builders<Order>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.OrderNumber),
                    new CreateIndexOptions { Unique = true }),
                new CreateIndexModel<Order>(
                    Builders<Order>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.Status)),
                new CreateIndexModel<Order>(
                    Builders<Order>.IndexKeys.Ascending(x => x.TenantId).Descending(x => x.CreatedAtUtc))
            ],
            cancellationToken);
    }

    private async Task CreatePaymentIndexes(CancellationToken cancellationToken)
    {
        var intents = dbContext.Collection<PaymentIntent>();
        await intents.Indexes.CreateOneAsync(
            new CreateIndexModel<PaymentIntent>(
                Builders<PaymentIntent>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.GatewayReference),
                new CreateIndexOptions { Unique = true, Sparse = true }),
            cancellationToken: cancellationToken);

        var transactions = dbContext.Collection<PaymentTransaction>();
        await transactions.Indexes.CreateOneAsync(
            new CreateIndexModel<PaymentTransaction>(
                Builders<PaymentTransaction>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.GatewayReference),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);
    }

    private async Task CreateDiscountIndexes(CancellationToken cancellationToken)
    {
        var discounts = dbContext.Collection<Discount>();
        await discounts.Indexes.CreateOneAsync(
            new CreateIndexModel<Discount>(
                Builders<Discount>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.CouponCode),
                new CreateIndexOptions { Unique = true, Sparse = true }),
            cancellationToken: cancellationToken);
    }

    private async Task CreateIntegrationIndexes(CancellationToken cancellationToken)
    {
        var connections = dbContext.Collection<IntegrationProviderConnection>();
        await connections.Indexes.CreateOneAsync(
            new CreateIndexModel<IntegrationProviderConnection>(
                Builders<IntegrationProviderConnection>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.ProviderKey),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        var jobs = dbContext.Collection<IntegrationSyncJob>();
        await jobs.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<IntegrationSyncJob>(
                    Builders<IntegrationSyncJob>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.Status)),
                new CreateIndexModel<IntegrationSyncJob>(
                    Builders<IntegrationSyncJob>.IndexKeys.Ascending(x => x.Status).Ascending(x => x.CreatedAtUtc))
            ],
            cancellationToken);
    }

}
