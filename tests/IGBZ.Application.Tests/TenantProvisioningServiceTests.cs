namespace IGBZ.Application.Tests;

using IGBZ.Application.Auth;
using IGBZ.Application.Provisioning;
using IGBZ.Application.Tenancy;
using IGBZ.Application.Tests.Fakes;
using IGBZ.Domain.Plans;
using IGBZ.Domain.Tenants;
using Xunit;

public class TenantProvisioningServiceTests
{
    private readonly TenantContext _tenantContext = new();

    private (TenantProvisioningService service, FakeRepository<Tenant> tenants, FakeRepository<TenantPlan> plans,
        FakeTenantScopedRepository<IGBZ.Domain.Plans.TenantStoreSubscription> subscriptions,
        FakeTenantScopedRepository<IGBZ.Domain.Customers.Customer> customers) Build(bool withPaidPlan = true)
    {
        var tenants = new FakeRepository<Tenant>();
        var plans = new FakeRepository<TenantPlan>();
        var subscriptions = new FakeTenantScopedRepository<IGBZ.Domain.Plans.TenantStoreSubscription>(_tenantContext);
        var customers = new FakeTenantScopedRepository<IGBZ.Domain.Customers.Customer>(_tenantContext);

        plans.Store.Add(new TenantPlan
        {
            Id = "plan-1",
            Name = "نقره‌ای",
            SystemName = "silver",
            PriceMonthlyToman = 890_000,
            PriceSixMonthsToman = 4_800_000,
            PriceYearlyToman = 8_900_000,
            DisplayOrder = 10,
            IsActive = true,
            TrialDurationDays = withPaidPlan ? 0 : 7
        });

        var authService = new AuthService(customers, new FakeJwtTokenService(), new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));
        var service = new TenantProvisioningService(tenants, plans, subscriptions, _tenantContext, authService);

        return (service, tenants, plans, subscriptions, customers);
    }

    [Fact]
    public async Task Provision_PaidPlan_CreatesTenantAndPendingSubscription()
    {
        var (service, tenants, _, subscriptions, customers) = Build();

        var result = await service.ProvisionAsync(new ProvisionTenantRequest
        {
            Subdomain = "ModStyle",
            StoreName = "مد استایل",
            AdminEmail = "owner@modstyle.ir",
            AdminPassword = "secret-123",
            PlanId = "plan-1"
        });

        Assert.True(result.Success);
        Assert.Equal("modstyle", result.TenantId);
        Assert.True(result.RequiresPayment);
        Assert.Equal(890_000m, result.AmountToman);
        Assert.NotNull(result.AccessToken);

        var tenant = tenants.Store.Single();
        Assert.Equal("modstyle", tenant.TenantId);
        Assert.Equal(TenantStatus.Trial, tenant.Status);
        Assert.Single(tenant.Domains);
        Assert.Equal("modstyle.market.com", tenant.Domains[0].HostName);

        var subscription = subscriptions.Store.Single();
        Assert.Equal(SubscriptionStatus.PendingPayment, subscription.Status);

        var owner = customers.Store.Single();
        Assert.True(owner.IsTenantOwner);
        Assert.Equal("owner@modstyle.ir", owner.Email);
    }

    [Fact]
    public async Task Provision_TrialPlan_ActivatesImmediately()
    {
        var (service, tenants, _, subscriptions, _) = Build(withPaidPlan: false);

        var result = await service.ProvisionAsync(new ProvisionTenantRequest
        {
            Subdomain = "trialstore",
            StoreName = "فروشگاه آزمایشی",
            AdminEmail = "a@b.ir",
            AdminPassword = "secret-123"
        });

        Assert.True(result.Success);
        Assert.False(result.RequiresPayment);

        Assert.Equal(TenantStatus.Active, tenants.Store.Single().Status);
        Assert.Equal(SubscriptionStatus.Trial, subscriptions.Store.Single().Status);
        Assert.NotNull(subscriptions.Store.Single().TrialEndDateUtc);
    }

    [Fact]
    public async Task Provision_DuplicateSubdomain_Fails()
    {
        var (service, _, _, _, _) = Build();
        await service.ProvisionAsync(new ProvisionTenantRequest
        {
            Subdomain = "dup",
            StoreName = "اول",
            AdminEmail = "a@b.ir",
            AdminPassword = "secret-123"
        });

        var second = await service.ProvisionAsync(new ProvisionTenantRequest
        {
            Subdomain = "dup",
            StoreName = "دوم",
            AdminEmail = "c@d.ir",
            AdminPassword = "secret-123"
        });

        Assert.False(second.Success);
        Assert.Contains("رزرو", second.ErrorMessage);
    }

    [Fact]
    public async Task IsSubdomainAvailable_RejectsReservedWords()
    {
        var (service, _, _, _, _) = Build();

        Assert.False(await service.IsSubdomainAvailableAsync("admin"));
        Assert.False(await service.IsSubdomainAvailableAsync("api"));
        Assert.False(await service.IsSubdomainAvailableAsync("platform"));
    }

    [Fact]
    public async Task IsSubdomainAvailable_RejectsInvalidChars()
    {
        var (service, _, _, _, _) = Build();

        Assert.False(await service.IsSubdomainAvailableAsync("bad name!"));
        Assert.False(await service.IsSubdomainAvailableAsync(new string('a', 61)));
    }
}
