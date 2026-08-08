namespace IGBZ.Application.Tenancy;

/// <summary>
/// وقتی عملیات تننت‌محور بدون بافت تننت فراخوانی شود پرتاب می‌شود.
/// مهم: این یک Exception است، نه نتیجهٔ خالی — تا باگ جداسازی در توسعه فوراً دیده شود (سند بخش ۵).
/// </summary>
public class TenantRequiredException : InvalidOperationException
{
    public TenantRequiredException(string operation)
        : base($"عملیات «{operation}» نیازمند بافت تننت (TenantContext) است؛ اما هیچ تننتی تنظیم نشده بود.")
    {
    }
}
