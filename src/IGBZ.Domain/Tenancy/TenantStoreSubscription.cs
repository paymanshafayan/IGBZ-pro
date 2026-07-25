using IGBZ.Domain.Shared;

namespace IGBZ.Domain.Tenancy;

public enum TenantSubscriptionStatus
{
    PendingPayment = 0,
    Active = 1,
    Expired = 2,
    Cancelled = 3
}

public sealed class TenantStoreSubscription : Entity
{
    private TenantStoreSubscription()
    {
    }

    private TenantStoreSubscription(string tenantId, string planCode, DateTimeOffset startsAtUtc, DateTimeOffset expiresAtUtc, DateTimeOffset now)
        : base(now)
    {
        TenantId = Guard.AgainstEmpty(tenantId, nameof(tenantId));
        PlanCode = Guard.AgainstEmpty(planCode, nameof(planCode)).ToLowerInvariant();
        StartsAtUtc = startsAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        Status = TenantSubscriptionStatus.PendingPayment;
    }

    public string TenantId { get; private set; } = string.Empty;
    public string PlanCode { get; private set; } = string.Empty;
    public TenantSubscriptionStatus Status { get; private set; }
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public static TenantStoreSubscription CreateMonthly(string tenantId, string planCode, DateTimeOffset now)
        => new(tenantId, planCode, now, now.AddMonths(1), now);

    public void Activate(DateTimeOffset now)
    {
        Status = TenantSubscriptionStatus.Active;
        Touch(now);
    }
}
