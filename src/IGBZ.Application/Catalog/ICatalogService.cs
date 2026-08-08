namespace IGBZ.Application.Catalog;

/// <summary>خلاصهٔ محصول برای لیست (گرید فروشگاه).</summary>
public class ProductListDto
{
    public string Id { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public decimal PriceToman { get; init; }
    public decimal? OldPriceToman { get; init; }

    /// <summary>فقط برای پنل ادمین — وضعیت انتشار (در کاتالوگ عمومی همیشه true است).</summary>
    public bool IsPublished { get; init; } = true;
}

/// <summary>جزئیات کامل محصول برای صفحهٔ محصول.</summary>
public class ProductDetailDto
{
    public string Id { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public List<string> Images { get; init; } = new();
    public List<ProductVariantDto> Variants { get; init; } = new();
    public bool IsDigital { get; init; }
}

public class ProductVariantDto
{
    public string Sku { get; init; } = string.Empty;
    public Dictionary<string, string> Attributes { get; init; } = new();
    public decimal PriceToman { get; init; }
    public decimal? OldPriceToman { get; init; }
    public int AvailableQuantity { get; init; }
}

/// <summary>کاتالوگ عمومی فروشگاه — فقط محصولات منتشرشدهٔ تننت جاری.</summary>
public interface ICatalogService
{
    Task<IReadOnlyList<ProductListDto>> GetPublishedProductsAsync(CancellationToken cancellationToken = default);

    Task<ProductDetailDto?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
