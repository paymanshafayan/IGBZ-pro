using IGBZ.Domain.Catalog;
using IGBZ.Domain.Discounts;
using IGBZ.Domain.Integrations;
using IGBZ.Domain.Inventory;
using IGBZ.Domain.Identity;
using IGBZ.Domain.Orders;
using IGBZ.Domain.Payments;
using IGBZ.Domain.Tenancy;

namespace IGBZ.Infrastructure.MongoDb;

public static class MongoCollectionNames
{
    public static string For<T>() => For(typeof(T));

    public static string For(Type type)
    {
        if (type == typeof(Tenant)) return "tenants";
        if (type == typeof(TenantPlan)) return "tenant_plans";
        if (type == typeof(TenantStoreSubscription)) return "tenant_subscriptions";
        if (type == typeof(StoreDomainMapping)) return "store_domain_mappings";
        if (type == typeof(User)) return "users";
        if (type == typeof(OtpChallenge)) return "otp_challenges";
        if (type == typeof(Product)) return "products";
        if (type == typeof(Category)) return "categories";
        if (type == typeof(InventoryItem)) return "inventory_items";
        if (type == typeof(StockReservation)) return "stock_reservations";
        if (type == typeof(Order)) return "orders";
        if (type == typeof(Discount)) return "discounts";
        if (type == typeof(PaymentIntent)) return "payment_intents";
        if (type == typeof(PaymentTransaction)) return "payment_transactions";
        if (type == typeof(IntegrationProviderConnection)) return "integration_connections";
        if (type == typeof(IntegrationSyncJob)) return "integration_sync_jobs";

        return type.Name.ToLowerInvariant();
    }
}
