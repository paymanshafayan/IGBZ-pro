namespace IGBZ.Domain.Plans;

/// <summary>اشتراک جاری یک فروشگاه (تننت) در یک پلن.</summary>
public class TenantStoreSubscription : Entity, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;

    /// <summary>شناسهٔ فروشگاه (StoreId) — در فاز ۱ همان TenantId است.</summary>
    public string StoreId { get; set; } = string.Empty;

    public string TenantPlanId { get; set; } = string.Empty;

    /// <summary>مالک فروشگاه — برای صدور فاکتور/اطلاع‌رسانی.</summary>
    public string OwnerCustomerId { get; set; } = string.Empty;

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.PendingPayment;

    public DateTime? TrialEndDateUtc { get; set; }
    public DateTime StartDateUtc { get; set; } = DateTime.UtcNow;
    public DateTime NextBillingDateUtc { get; set; }
    public bool AutoRenew { get; set; }

    public bool IsUsable(DateTime nowUtc) =>
        Status == SubscriptionStatus.Active
        || (Status == SubscriptionStatus.Trial
            && TrialEndDateUtc.HasValue
            && TrialEndDateUtc.Value > nowUtc);

    public void Activate()
    {
        Status = SubscriptionStatus.Active;
        UpdatedOnUtc = DateTime.UtcNow;
    }
}

public enum SubscriptionStatus
{
    PendingPayment = 0,
    Trial = 10,
    Active = 20,
    PastDue = 30,
    Suspended = 40,
    Cancelled = 50
}
