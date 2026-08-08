namespace IGBZ.Domain.Instagram;

/// <summary>
/// دفترکل پاداش «فالو + منشن استوری → کد تخفیف در دایرکت» (سند بخش ۱۳).
/// هر ردیف = یک کد تخفیف صادرشده به یک کاربر اینستاگرام در یک فروشگاه؛
/// Unique روی (TenantId, InstagramScopedId) ضد تکرار.
/// </summary>
public class InstagramFollowMentionReward : Entity, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;

    /// <summary>شناسهٔ Scoped کاربر منشن‌کننده (IGSID) — نه Username (تغییر می‌کند).</summary>
    public string InstagramScopedId { get; set; } = string.Empty;

    /// <summary>کد تخفیف واقعی nopCommerce-like (Discount با CouponCode).</summary>
    public string CouponCode { get; set; } = string.Empty;

    public bool DirectMessageSent { get; set; }

    /// <summary>اگر دایرکت شکست خورد، کامنت عمومی (بدون کد) گذاشته شد.</summary>
    public bool FallbackCommentPosted { get; set; }

    public DateTime IssuedOnUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>اتصال حساب اینستاگرام کاربر به حساب مشتری (برای مسابقه/حمایت مالی).</summary>
public class InstagramAccountLink : Entity, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string InstagramScopedId { get; set; } = string.Empty;
    public string? Username { get; set; }
    public DateTime LinkedOnUtc { get; set; } = DateTime.UtcNow;
}
