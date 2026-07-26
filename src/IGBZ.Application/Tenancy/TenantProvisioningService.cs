using IGBZ.Application.Abstractions;
using IGBZ.Domain.Identity;
using IGBZ.Domain.Shared;
using IGBZ.Domain.Tenancy;

namespace IGBZ.Application.Tenancy;

public sealed class TenantProvisioningService(
    IPlatformRepository<Tenant> tenants,
    IPlatformRepository<TenantPlan> plans,
    IPlatformRepository<TenantStoreSubscription> subscriptions,
    IPlatformRepository<StoreDomainMapping> domainMappings,
    IPlatformRepository<User> users,
    IPlatformDomainProvider domainProvider,
    IClock clock)
{
    public async Task<IReadOnlyList<TenantPlanDto>> ListPlansAsync(CancellationToken cancellationToken = default)
    {
        var result = await plans.ListAsync(plan => plan.IsActive, cancellationToken);
        return result
            .OrderBy(plan => plan.MonthlyPrice)
            .Select(ToDto)
            .ToList();
    }

    public async Task<TenantPlanDto> CreatePlanAsync(CreateTenantPlanRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await plans.FirstOrDefaultAsync(plan => plan.Code == request.Code.ToLowerInvariant(), cancellationToken);
        if (existing is not null)
        {
            throw new DomainException("A tenant plan with this code already exists.");
        }

        var plan = TenantPlan.Create(request.Code, request.Title, request.MonthlyPrice, request.Currency, clock.UtcNow);
        await plans.AddAsync(plan, cancellationToken);
        return ToDto(plan);
    }

    public async Task<CheckSubdomainResponse> CheckSubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
    {
        var normalized = Tenant.NormalizeSubdomain(subdomain);
        if (IsReservedSubdomain(normalized))
        {
            return new CheckSubdomainResponse(normalized, false, "This subdomain is reserved by the platform.");
        }

        var host = $"{normalized}.{StoreDomainMapping.NormalizeHost(domainProvider.RootDomain)}";
        var existing = await domainMappings.FirstOrDefaultAsync(mapping => mapping.Host == host, cancellationToken);
        return existing is null
            ? new CheckSubdomainResponse(normalized, true, null)
            : new CheckSubdomainResponse(normalized, false, "This subdomain is already taken.");
    }

    public async Task<ProvisionTenantResponse> ProvisionAsync(ProvisionTenantRequest request, CancellationToken cancellationToken = default)
    {
        var subdomainCheck = await CheckSubdomainAsync(request.Subdomain, cancellationToken);
        if (!subdomainCheck.IsAvailable)
        {
            throw new DomainException(subdomainCheck.Reason ?? "Subdomain is not available.");
        }

        var planCode = request.PlanCode.Trim().ToLowerInvariant();
        var plan = await plans.FirstOrDefaultAsync(candidate => candidate.Code == planCode && candidate.IsActive, cancellationToken);
        if (plan is null)
        {
            throw new DomainException("Selected tenant plan was not found or is inactive.");
        }

        var now = clock.UtcNow;
        var tenant = Tenant.Create(request.StoreName, subdomainCheck.Subdomain, request.AdminEmail, request.AdminPhone, now);
        var subscription = TenantStoreSubscription.CreateMonthly(tenant.Id, plan.Code, now);
        subscription.Activate(now);
        tenant.Activate(now);

        var mapping = StoreDomainMapping.CreatePlatformSubdomain(tenant.Id, tenant.Subdomain, domainProvider.RootDomain, now);
        var owner = User.CreateTenantOwner(tenant.Id, tenant.AdminPhone, tenant.AdminEmail, tenant.AdminEmail, now);
        owner.MarkPhoneVerified(now);

        await tenants.AddAsync(tenant, cancellationToken);
        await subscriptions.AddAsync(subscription, cancellationToken);
        await domainMappings.AddAsync(mapping, cancellationToken);
        await users.AddAsync(owner, cancellationToken);

        return new ProvisionTenantResponse(
            tenant.Id,
            tenant.StoreName,
            tenant.Subdomain,
            mapping.Host,
            plan.Code,
            tenant.Status.ToString());
    }

    private static TenantPlanDto ToDto(TenantPlan plan)
        => new(plan.Id, plan.Code, plan.Title, plan.MonthlyPrice, plan.Currency, plan.IsActive);

    private static bool IsReservedSubdomain(string subdomain)
    {
        string[] reserved = ["www", "api", "admin", "platform", "app", "cdn", "static", "mail", "support"];
        return reserved.Contains(subdomain, StringComparer.OrdinalIgnoreCase);
    }
}
