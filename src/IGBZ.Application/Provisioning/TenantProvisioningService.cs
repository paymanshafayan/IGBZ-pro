namespace IGBZ.Application.Provisioning;

using IGBZ.Application.Abstractions;
using IGBZ.Application.Auth;
using IGBZ.Application.Tenancy;
using IGBZ.Domain.Plans;
using IGBZ.Domain.Tenants;

public class TenantProvisioningService : ITenantProvisioningService
{
    private static readonly string[] ReservedSubdomains =
    {
        "admin", "api", "www", "app", "mail", "master", "dashboard", "billing", "platform", "www"
    };

    private readonly IRepository<Tenant> _tenantRepository;
    private readonly IRepository<TenantPlan> _planRepository;
    private readonly ITenantScopedRepository<TenantStoreSubscription> _subscriptionRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IAuthService _authService;

    public TenantProvisioningService(
        IRepository<Tenant> tenantRepository,
        IRepository<TenantPlan> planRepository,
        ITenantScopedRepository<TenantStoreSubscription> subscriptionRepository,
        ITenantContext tenantContext,
        IAuthService authService)
    {
        _tenantRepository = tenantRepository;
        _planRepository = planRepository;
        _subscriptionRepository = subscriptionRepository;
        _tenantContext = tenantContext;
        _authService = authService;
    }

    public async Task<bool> IsSubdomainAvailableAsync(string subdomain, CancellationToken cancellationToken = default)
    {
        var slug = NormalizeSubdomain(subdomain);
        if (slug is null)
            return false;

        if (ReservedSubdomains.Contains(slug, StringComparer.Ordinal))
            return false;

        var existing = await _tenantRepository.FirstOrDefaultAsync(t => t.TenantId == slug, cancellationToken);
        return existing == null;
    }

    public async Task<ProvisioningResult> ProvisionAsync(ProvisionTenantRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.StoreName))
            return new ProvisioningResult { Success = false, ErrorMessage = "نام فروشگاه الزامی است." };

        var slug = NormalizeSubdomain(request.Subdomain);
        if (slug is null)
            return new ProvisioningResult { Success = false, ErrorMessage = "زیردامنهٔ نامعتبر است." };

        if (!await IsSubdomainAvailableAsync(slug, cancellationToken))
            return new ProvisioningResult { Success = false, ErrorMessage = "این زیردامنه قبلاً رزرو شده است." };

        // ۱) پلن: پلن مشخص یا اولین پلن فعال
        TenantPlan? plan = null;
        if (!string.IsNullOrWhiteSpace(request.PlanId))
            plan = await _planRepository.GetByIdAsync(request.PlanId, cancellationToken);
        plan ??= (await _planRepository.FindAsync(p => p.IsActive, cancellationToken))
            .OrderBy(p => p.DisplayOrder)
            .FirstOrDefault();

        if (plan == null)
            return new ProvisioningResult { Success = false, ErrorMessage = "هیچ پلن فعالی برای ثبت‌نام موجود نیست." };

        // ۲) ساخت Tenant
        var tenant = new Tenant
        {
            TenantId = slug,
            DisplayName = request.StoreName.Trim(),
            PlanId = plan.Id,
            Status = TenantStatus.Trial,
            CreatedOnUtc = DateTime.UtcNow
        };
        tenant.Domains.Add(new TenantDomain
        {
            HostName = $"{slug}.market.com",
            IsPrimary = true,
            IsSslVerified = false
        });
        await _tenantRepository.InsertAsync(tenant, cancellationToken);

        // ۳) ساخت حساب مالک (در بافت تننت جدید)
        _tenantContext.Set(slug);
        var ownerResult = await _authService.RegisterAsync(new RegisterCustomerRequest
        {
            TenantId = slug,
            Email = request.AdminEmail,
            Password = request.AdminPassword,
            Phone = request.AdminPhone,
            IsTenantOwner = true
        }, cancellationToken);

        if (!ownerResult.Success)
            return new ProvisioningResult { Success = false, ErrorMessage = ownerResult.ErrorMessage };

        // ۴) ساخت اشتراک — Trial همان‌لحظه، پولی PendingPayment
        var isTrial = plan.TrialDurationDays > 0;
        var now = DateTime.UtcNow;
        var subscription = new TenantStoreSubscription
        {
            TenantId = slug,
            StoreId = slug,
            TenantPlanId = plan.Id,
            OwnerCustomerId = ownerResult.Customer!.Id,
            Status = isTrial ? SubscriptionStatus.Trial : SubscriptionStatus.PendingPayment,
            StartDateUtc = now,
            TrialEndDateUtc = isTrial ? now.AddDays(plan.TrialDurationDays) : null,
            NextBillingDateUtc = now.AddDays(plan.GetDurationDays(BillingCycle.Monthly)),
            AutoRenew = false,
            CreatedOnUtc = now
        };
        await _subscriptionRepository.InsertAsync(subscription, cancellationToken);

        if (isTrial)
            tenant.Activate();
        else
            tenant.Status = TenantStatus.Trial; // در انتظار پرداخت

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);

        return new ProvisioningResult
        {
            Success = true,
            TenantId = slug,
            SubscriptionId = subscription.Id,
            RequiresPayment = !isTrial,
            AmountToman = isTrial ? 0 : plan.GetPriceToman(BillingCycle.Monthly),
            OwnerCustomerId = ownerResult.Customer.Id,
            AccessToken = ownerResult.AccessToken
        };
    }

    private static string? NormalizeSubdomain(string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
            return null;

        var cleaned = subdomain.Trim().ToLowerInvariant();
        if (cleaned.Length > 60)
            return null;

        // فقط حروف، اعداد و خط تیره
        foreach (var c in cleaned)
        {
            if (!char.IsLetterOrDigit(c) && c != '-')
                return null;
        }

        return cleaned;
    }
}
