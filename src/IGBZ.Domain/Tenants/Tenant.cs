namespace IGBZ.Domain.Tenants;

public class Tenant : Entity
{
    /// <summary>slug تننت — همان شناسهٔ جداسازی (مطابق سند بخش ۶.۱).</summary>
    public string TenantId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>شناسهٔ پلن فعال (انتخابی تا زمان Provisioning).</summary>
    public string? PlanId { get; set; }

    public TenantStatus Status { get; set; } = TenantStatus.Trial;

    public List<TenantDomain> Domains { get; set; } = new();

    public TenantSettings Settings { get; set; } = new();

    public void Activate()
    {
        if (Status == TenantStatus.Suspended)
            throw new InvalidOperationException("تننت تعلیق‌شده را نمی‌توان مستقیم فعال کرد.");
        Status = TenantStatus.Active;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public void Suspend(string? reason)
    {
        Status = TenantStatus.Suspended;
        UpdatedOnUtc = DateTime.UtcNow;
    }
}

public enum TenantStatus
{
    Trial = 0,
    Active = 10,
    PastDue = 20,
    Suspended = 30,
    Cancelled = 40
}

public class TenantDomain
{
    public string HostName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsSslVerified { get; set; }
}

public class TenantSettings
{
    public string Currency { get; set; } = "IRT";
    public decimal TaxRatePercent { get; set; } = 9;
}
