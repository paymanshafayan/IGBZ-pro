namespace IGBZ.Domain.Catalog;

/// <summary>
/// محصول یک تننت — با Variant ها (SKU/قیمت/موجودی/رزرو).
/// رزرو اتمیک موجودی در لایهٔ زیرساخت با <c>findOneAndUpdate</c> انجام می‌شود (سند بخش ۷.۳).
/// </summary>
public class Product : Entity, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>نامک/اسلاگ برای URL — یکتا در سطح تننت.</summary>
    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<string> CategoryIds { get; set; } = new();

    public List<ProductVariant> Variants { get; set; } = new();

    public List<string> Images { get; set; } = new();

    public bool IsPublished { get; set; }

    /// <summary>حذف نرم — محصولات حذف‌شده در کاتالوگ نمایش داده نمی‌شوند.</summary>
    public bool Deleted { get; set; }

    public bool IsDigital { get; set; }

    /// <summary>برای محصولات دیجیتال — ارجاع به فایل دانلودی.</summary>
    public string? DownloadRef { get; set; }

    // ── سئو (تولید خودکار در فاز ۹) ──
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }

    /// <summary>ترجمه‌های خودکار محصول (زبان‌های دیگر) — سند بخش ۱۴.</summary>
    public List<ProductTranslation> Translations { get; set; } = new();

    public ProductVariant? GetVariant(string sku) =>
        Variants.FirstOrDefault(v => string.Equals(v.Sku, sku, StringComparison.OrdinalIgnoreCase));

    public decimal GetLowestPriceToman() =>
        Variants.Count == 0 ? 0 : Variants.Min(v => v.PriceToman);

    public bool IsAvailable(string sku, int quantity)
    {
        var variant = GetVariant(sku);
        return variant != null && variant.AvailableQuantity >= quantity;
    }
}

/// <summary>ترجمهٔ خودکار نام/توضیحات محصول به یک زبان.</summary>
public class ProductTranslation
{
    public string LanguageCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
}
