namespace IGBZ.Application.Tenancy;

/// <summary>
/// بافت تننت جاری درخواست — از JWT Claim (موبایل) یا Host Header (وب) تزریق می‌شود.
/// همهٔ Repository های تننت‌محور بدون مقدار آن خطا می‌دهند (سند بخش ۵).
/// </summary>
public interface ITenantContext
{
    string? TenantId { get; }
    void Set(string tenantId);
    void Clear();
}
