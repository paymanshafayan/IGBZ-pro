namespace IGBZ.Application.Provisioning;

/// <summary>درخواست ساخت فروشگاه (تننت) جدید — از سایت مادر یا ویزارد.</summary>
public class ProvisionTenantRequest
{
    public string Subdomain { get; init; } = string.Empty;
    public string StoreName { get; init; } = string.Empty;
    public string AdminEmail { get; init; } = string.Empty;
    public string AdminPassword { get; init; } = string.Empty;
    public string? AdminPhone { get; init; }

    /// <summary>شناسهٔ پلن — اگر null باشد، اولین پلن فعال استفاده می‌شود.</summary>
    public string? PlanId { get; init; }
}

public class ProvisioningResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? TenantId { get; init; }
    public string? SubscriptionId { get; init; }
    public bool RequiresPayment { get; init; }
    public decimal AmountToman { get; init; }
    public string? OwnerCustomerId { get; init; }
    public string? AccessToken { get; init; }
}

/// <summary>
/// Provisioning (سند بخش ۱۱): اعتبارسنجی زیردامنه → ساخت Tenant → ساخت مالک → ساخت اشتراک
/// (Trial همان‌لحظه فعال؛ پولی PendingPayment تا پرداخت).
/// </summary>
public interface ITenantProvisioningService
{
    Task<bool> IsSubdomainAvailableAsync(string subdomain, CancellationToken cancellationToken = default);

    Task<ProvisioningResult> ProvisionAsync(ProvisionTenantRequest request, CancellationToken cancellationToken = default);
}
