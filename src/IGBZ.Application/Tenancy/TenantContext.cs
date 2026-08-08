namespace IGBZ.Application.Tenancy;

/// <summary>
/// پیاده‌سازی مبتنی بر AsyncLocal — بافت تننت در طول زنجیرهٔ async جریان می‌یابد
/// و با پایان درخواست پاک می‌شود.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    private static readonly AsyncLocal<string?> _current = new();

    public string? TenantId => _current.Value;

    public void Set(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("شناسهٔ تننت نمی‌تواند خالی باشد.", nameof(tenantId));
        _current.Value = tenantId;
    }

    public void Clear() => _current.Value = null;
}
