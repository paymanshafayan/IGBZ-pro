namespace IGBZ.Domain.Common;

/// <summary>
/// هر موجودیتی که به یک تننت تعلق دارد باید این اینترفیس را پیاده کند.
/// وجود فیلد <c>TenantId</c> در سطح تایپ، قفل کامپایل‌تایم برای جداسازی چندمستأجری را ممکن می‌کند
/// (سند معماری بخش ۵): هیچ Query بدون tenantId پذیرفته نیست.
/// </summary>
public interface ITenantEntity
{
    string TenantId { get; set; }
}
