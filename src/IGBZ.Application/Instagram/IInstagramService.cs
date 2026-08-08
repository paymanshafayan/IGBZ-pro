namespace IGBZ.Application.Instagram;

/// <summary>نتیجهٔ پردازش یک رویداد وب‌هوک.</summary>
public class InstagramWebhookResult
{
    public bool Processed { get; init; }
    public string? Message { get; init; }
}

/// <summary>نتیجهٔ انتشار پست خودکار محصول.</summary>
public class InstagramPostResult
{
    public bool IsSuccess { get; init; }
    public string? Message { get; init; }
}

/// <summary>
/// دستیار اینستاگرام (سند بخش ۱۳):
/// - وب‌هوک متا (comments/mentions) با اعتبارسنجی امضای HMAC-SHA256
/// - کامنت → تطبیق SKU → ارسال دایرکت لینک خرید
/// - فالو + منشن استوری → بررسی فالو واقعی → صدور کد تخفیف → دایرکت
/// - انتشار خودکار پست محصول جدید
/// قاعدهٔ سخت: هر فراخوانی Graph API واقعی است و نتیجه از پاسخ واقعی خوانده می‌شود.
/// </summary>
public interface IInstagramService
{
    /// <summary>اعتبارسنجی امضای وب‌هوک متا (X-Hub-Signature-256) با App Secret.</summary>
    bool VerifyWebhookSignature(string rawBody, string signatureHeader);

    /// <summary>پردازش وب‌هوک کامنت — تطبیق SKU و ارسال دایرکت لینک خرید.</summary>
    Task<InstagramWebhookResult> ProcessCommentAsync(
        string tenantId, string commentId, string commentText, string commenterIgsid,
        CancellationToken cancellationToken = default);

    /// <summary>پردازش وب‌هوک منشن — بررسی فالو + صدور کد تخفیف + دایرکت.</summary>
    Task<InstagramWebhookResult> ProcessMentionAsync(
        string tenantId, string mentioningUserIgsid, string? mediaId,
        CancellationToken cancellationToken = default);

    /// <summary>انتشار خودکار پست محصول جدید (دو مرحله: ساخت Container + Publish).</summary>
    Task<InstagramPostResult> PublishProductPostAsync(
        string productId, string imageUrl, CancellationToken cancellationToken = default);
}
