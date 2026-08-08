namespace IGBZ.Application.Integration;

/// <summary>نوع یکپارچه‌سازی بیرونی (سند بخش ۱۰.۱).</summary>
public enum IntegrationProviderType
{
    Payment = 0,
    Shipping = 10,
    Marketplace = 20,
    Ai = 30,
    Ads = 40,
    Accounting = 50,
    Tax = 60,
    Sms = 70
}

/// <summary>نتیجهٔ تست اتصال یک Provider.</summary>
public class IntegrationTestResult
{
    public bool IsSuccess { get; init; }
    public string? Message { get; init; }
}

/// <summary>بافت همگام‌سازی (برای Sync/Publish).</summary>
public class IntegrationSyncContext
{
    public string TenantId { get; init; } = string.Empty;
    public string? EntityId { get; init; }
    public Dictionary<string, object?> Data { get; init; } = new();
}

/// <summary>
/// چارچوب یکپارچه‌سازی بیرونی (سند بخش ۱۰):
/// هر Provider با کلید یکتا (مثل "payir"، "digikala"، "kavenegar") و یک نوع ثبت می‌شود.
/// قاعدهٔ سخت (بخش ۱۰.۲): هیچ پیاده‌سازی‌ای بدون فراخوانی HTTP واقعی «موفق» اعلام نمی‌کند.
/// </summary>
public interface IIntegrationProvider
{
    string ProviderKey { get; }
    IntegrationProviderType Type { get; }
    Task<IntegrationTestResult> TestConnectionAsync(CancellationToken cancellationToken = default);
    Task<IntegrationSyncResult> SyncAsync(IntegrationSyncContext context, CancellationToken cancellationToken = default);
}

public class IntegrationSyncResult
{
    public bool IsSuccess { get; init; }
    public string? Message { get; init; }
}
