namespace IGBZ.Application.Tests;

using IGBZ.Application.Payments;
using IGBZ.Application.Subscriptions;
using IGBZ.Application.Tenancy;
using IGBZ.Application.Tests.Fakes;
using IGBZ.Domain.Common;
using IGBZ.Domain.Plans;
using IGBZ.Domain.Payments;
using IGBZ.Domain.Tenants;
using Xunit;

public class TenantSubscriptionServiceTests
{
    private readonly TenantContext _tenantContext = new();

    private (
        TenantSubscriptionService service,
        FakeTenantScopedRepository<TenantStoreSubscription> subscriptions,
        FakeRepository<Tenant> tenants,
        FakeTenantScopedRepository<PaymentTransactionLedger> ledger) Build()
    {
        _tenantContext.Set("t1");

        var subscriptions = new FakeTenantScopedRepository<TenantStoreSubscription>(_tenantContext);
        var tenants = new FakeRepository<Tenant>();
        var plans = new FakeRepository<TenantPlan>();
        plans.Store.Add(new TenantPlan
        {
            Id = "plan-1",
            Name = "نقره‌ای",
            SystemName = "silver",
            PriceMonthlyToman = 890_000,
            DisplayOrder = 10,
            IsActive = true
        });

        var ledger = new FakeTenantScopedRepository<PaymentTransactionLedger>(_tenantContext);
        var gateway = new FakePaymentGateway("test");
        var paymentService = new PaymentService(ledger, new[] { gateway });

        var service = new TenantSubscriptionService(subscriptions, plans, tenants, paymentService);
        return (service, subscriptions, tenants, ledger);
    }

    private static void SeedPendingSubscription(FakeTenantScopedRepository<TenantStoreSubscription> subscriptions)
    {
        subscriptions.Store.Add(new TenantStoreSubscription
        {
            Id = "sub-1",
            TenantId = "t1",
            StoreId = "t1",
            TenantPlanId = "plan-1",
            OwnerCustomerId = "c1",
            Status = SubscriptionStatus.PendingPayment,
            StartDateUtc = DateTime.UtcNow,
            NextBillingDateUtc = DateTime.UtcNow.AddDays(30),
            CreatedOnUtc = DateTime.UtcNow
        });
    }

    [Fact]
    public async Task GetStatus_NoSubscription_ReturnsFalse()
    {
        var (service, _, _, _) = Build();
        var status = await service.GetStatusAsync("t1");
        Assert.False(status.HasSubscription);
    }

    [Fact]
    public async Task Payment_ThenVerify_ActivatesSubscriptionAndTenant()
    {
        var (service, subscriptions, tenants, _) = Build();
        SeedPendingSubscription(subscriptions);

        tenants.Store.Add(new Tenant
        {
            Id = "tenant-1",
            TenantId = "t1",
            DisplayName = "فروشگاه",
            Status = TenantStatus.Trial
        });

        var request = await service.RequestPaymentAsync("t1", "test", "https://cb.test");
        Assert.True(request.IsSuccess);
        Assert.NotNull(request.TrackingNumber);

        var verify = await service.VerifyAndActivateAsync("t1", request.TrackingNumber!);

        Assert.True(verify.IsSuccess);
        Assert.Equal(SubscriptionStatus.Active, subscriptions.Store.Single().Status);
        Assert.Equal(TenantStatus.Active, tenants.Store.Single().Status);
    }

    [Fact]
    public async Task Verify_SecondTime_IsAlreadyProcessed_ButStillActive()
    {
        var (service, subscriptions, tenants, gateway) = Build();
        SeedPendingSubscription(subscriptions);
        tenants.Store.Add(new Tenant { Id = "t-1", TenantId = "t1", Status = TenantStatus.Trial });

        var request = await service.RequestPaymentAsync("t1", "test", "https://cb.test");
        await service.VerifyAndActivateAsync("t1", request.TrackingNumber!);
        var second = await service.VerifyAndActivateAsync("t1", request.TrackingNumber!);

        Assert.True(second.IsSuccess);
        Assert.True(second.AlreadyProcessed);
        Assert.Equal(SubscriptionStatus.Active, subscriptions.Store.Single().Status);
    }

    [Fact]
    public async Task Verify_Fails_WhenGatewayRejects()
    {
        var (service, subscriptions, _, _) = Build();
        SeedPendingSubscription(subscriptions);

        // هیچ پرداختی درخواست نشده — tracking نامعتبر
        var verify = await service.VerifyAndActivateAsync("t1", "non-existent");

        Assert.False(verify.IsSuccess);
        Assert.Equal(SubscriptionStatus.PendingPayment, subscriptions.Store.Single().Status);
    }
}
