namespace IGBZ.Application.Admin;

/// <summary>ورودی ساخت/ویرایش محصول توسط ادمین تننت.</summary>
public class AdminProductInput
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public List<string> Images { get; init; } = new();
    public bool IsPublished { get; init; }
    public bool IsDigital { get; init; }
    public List<AdminProductVariantInput> Variants { get; init; } = new();
}

public class AdminProductVariantInput
{
    public string Sku { get; init; } = string.Empty;
    public Dictionary<string, string> Attributes { get; init; } = new();
    public decimal PriceToman { get; init; }
    public decimal? OldPriceToman { get; init; }
    public int StockQuantity { get; init; }
}

/// <summary>مدیریت محصولات تننت (فقط مالک/ادمین) — ساخت، ویرایش، حذف نرم، انتشار.</summary>
public interface IAdminProductService
{
    /// <summary>لیست همهٔ محصولات (شامل منتشرنشده و حذف‌نشده) — برای پنل ادمین.</summary>
    Task<IReadOnlyList<ProductListDto>> ListAllAsync(CancellationToken cancellationToken = default);

    Task<string> CreateAsync(AdminProductInput input, CancellationToken cancellationToken = default);
    Task UpdateAsync(string productId, AdminProductInput input, CancellationToken cancellationToken = default);
    Task DeleteAsync(string productId, CancellationToken cancellationToken = default);
    Task SetPublishedAsync(string productId, bool published, CancellationToken cancellationToken = default);
}
