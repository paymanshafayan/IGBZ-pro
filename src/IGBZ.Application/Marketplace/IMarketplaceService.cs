namespace IGBZ.Application.Marketplace;

/// <summary>آیتم فید ترب — ساخته‌شده از کاتالوگ واقعی (سند بخش ۱۰.۲: داده از منبع واقعی).</summary>
public class TorobProductItem
{
    public string PageUniqueId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public decimal PriceToman { get; init; }
    public decimal? OldPriceToman { get; init; }
    public bool IsAvailability { get; init; }
    public string ProductUrl { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
}

public class TorobFeedResult
{
    public int TotalCount { get; init; }
    public IReadOnlyList<TorobProductItem> Products { get; init; } = Array.Empty<TorobProductItem>();
}

public class MarketplaceSyncResult
{
    public bool IsSuccess { get; init; }
    public string? Message { get; init; }
}

public class DivarPostResult
{
    public bool IsSuccess { get; init; }
    public string? PostToken { get; init; }
    public string? PostUrl { get; init; }
    public string? Message { get; init; }
}

/// <summary>
/// مارکت‌پلیس (سند بخش ۱۰.۴): فید ترب، همگام‌سازی دیجی‌کالا، انتشار در دیوار.
/// قاعدهٔ سخت: هر فراخوانی بیرونی واقعی است و نتیجه از کد وضعیت واقعی خوانده می‌شود.
/// </summary>
public interface IMarketplaceService
{
    /// <summary>فید JSON ترب از کاتالوگ واقعی فروشگاه (برای موتور قیمت‌یاب ترب).</summary>
    Task<TorobFeedResult> GetTorobFeedAsync(CancellationToken cancellationToken = default);

    /// <summary>همگام‌سازی موجودی/قیمت با دیجی‌کالا (با توکن فروشنده از اعتبارنامهٔ تننت).</summary>
    Task<MarketplaceSyncResult> SyncStockAndPriceWithDigikalaAsync(
        string sellerVariantId, int stockCount, decimal priceToman,
        CancellationToken cancellationToken = default);

    /// <summary>انتشار آگهی در دیوار (Kenar Divar API) با توکن از اعتبارنامه.</summary>
    Task<DivarPostResult> PublishPostOnDivarAsync(
        string title, string description, decimal priceToman, string? imageUrl,
        CancellationToken cancellationToken = default);
}
