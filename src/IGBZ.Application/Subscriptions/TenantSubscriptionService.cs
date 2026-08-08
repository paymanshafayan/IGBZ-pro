namespace IGBZ.Application.Subscriptions;

using IGBZ.Application.Abstractions;
using IGBZ.Application.Payments;
using IGBZ.Domain.Common;
using IGBZ.Domain.Plans;
using IGBZ.Domain.Tenants;

public class TenantSubscriptionService : ITenantSubscriptionService
{
    private readonly ITenantScopedRepository<TenantStoreSubscription> _subscriptionRepository;
    private readonly IRepository<TenantPlan> _planRepository;
    private readonly IRepository<Tenant> _tenantRepository;
    private readonly IPaymentService _paymentService;

    public TenantSubscriptionService(
        ITenantScopedRepository<TenantStoreSubscription> subscriptionRepository,
        IRepository<TenantPlan> planRepository,
        IRepository<Tenant> tenantRepository,
        IPaymentService paymentService)
    {
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
        _tenantRepository = tenantRepository;
        _paymentService = paymentService;
    }

    public async Task<SubscriptionStatusResult> GetStatusAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var subscription = await GetLatestAsync(tenantId, cancellationToken);
        if (subscription == null)
            return new SubscriptionStatusResult { HasSubscription = false, Status = nameof(SubscriptionStatus.PendingPayment) };

        var plan = await _planRepository.GetByIdAsync(subscription.TenantPlanId, cancellationToken);

        return new SubscriptionStatusResult
        {
            HasSubscription = true,
            IsUsable = subscription.IsUsable(DateTime.UtcNow),
            PlanName = plan?.Name,
            Status = subscription.Status.ToString(),
            NextBillingDateUtc = subscription.NextBillingDateUtc
        };
    }

    public async Task<PaymentRequestResult> RequestPaymentAsync(
        string tenantId, string gatewayName, string callbackUrl, CancellationToken cancellationToken = default)
    {
        var subscription = await GetLatestAsync(tenantId, cancellationToken)
            ?? throw new InvalidOperationException("برای این فروشگاه اشتراکی ثبت نشده است.");

        var plan = await _planRepository.GetByIdAsync(subscription.TenantPlanId, cancellationToken)
            ?? throw new InvalidOperationException("پلن اشتراک یافت نشد.");

        var amount = new Money(plan.GetPriceToman(BillingCycle.Monthly));
        if (amount.IsZero)
            throw new InvalidOperationException("این پلن هزینه‌ای ندارد (پلن آزمایشی).");

        return await _paymentService.RequestAsync(subscription.Id, gatewayName, amount, callbackUrl, cancellationToken);
    }

    public async Task<PaymentVerifyResult> VerifyAndActivateAsync(
        string tenantId, string trackingNumber, CancellationToken cancellationToken = default)
    {
        var subscription = await GetLatestAsync(tenantId, cancellationToken)
            ?? throw new InvalidOperationException("برای این فروشگاه اشتراکی ثبت نشده است.");

        var plan = await _planRepository.GetByIdAsync(subscription.TenantPlanId, cancellationToken)
            ?? throw new InvalidOperationException("پلن اشتراک یافت نشد.");

        var expectedAmount = new Money(plan.GetPriceToman(BillingCycle.Monthly));
        var verify = await _paymentService.VerifyAsync(trackingNumber, expectedAmount, cancellationToken);

        if (verify.IsSuccess)
            await ActivateAsync(tenantId, cancellationToken);

        return verify;
    }

    public async Task<bool> ActivateAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var subscription = await GetLatestAsync(tenantId, cancellationToken);
        if (subscription == null)
            return false;

        if (subscription.Status != SubscriptionStatus.Active)
        {
            subscription.Activate();
            await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
        }

        var tenant = await _tenantRepository.FirstOrDefaultAsync(t => t.TenantId == tenantId, cancellationToken);
        if (tenant != null && tenant.Status != TenantStatus.Active)
        {
            tenant.Activate();
            await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        }

        return true;
    }

    private async Task<TenantStoreSubscription?> GetLatestAsync(string tenantId, CancellationToken cancellationToken)
    {
        var all = await _subscriptionRepository.FindAsync(s => s.TenantId == tenantId, cancellationToken);
        return all.OrderByDescending(s => s.CreatedOnUtc).FirstOrDefault();
    }
}
