namespace IGBZ.Application.AiStudio;

/// <summary>نتیجهٔ یک عملیات استودیوی AI.</summary>
public class AiStudioResult
{
    public bool IsSuccess { get; init; }
    public string? OutputUrl { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>درخواست ادیت عکس محصول (حذف پس‌زمینه/استودیویی).</summary>
public class EnhancePhotoRequest
{
    public string ImageUrl { get; init; } = string.Empty;
    public string? BackgroundPreset { get; init; }
    public string? SkuCode { get; init; }
}

/// <summary>درخواست ویدیوی کوتاه استوری.</summary>
public class VideoStoryRequest
{
    public string ProductId { get; init; } = string.Empty;
    public string ProductTitle { get; init; } = string.Empty;
    public decimal PriceToman { get; init; }
    public string? BackgroundMusicTrackId { get; init; }
}

/// <summary>درخواست صداپیشگی فارسی.</summary>
public class VoiceOverRequest
{
    public string Text { get; init; } = string.Empty;
    public string SpeakerGender { get; init; } = "Female";
}

/// <summary>درخواست ترجمهٔ خودکار.</summary>
public class TranslateRequest
{
    public string Text { get; init; } = string.Empty;
    public string TargetLanguageCode { get; init; } = "en";
}

public class TranslateResult
{
    public bool IsSuccess { get; init; }
    public string? TranslatedText { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>نتیجهٔ تولید متادیتای سئو — قطعی/محلی (نیاز به API ندارد).</summary>
public class SeoMetaResult
{
    public string MetaTitle { get; init; } = string.Empty;
    public string MetaDescription { get; init; } = string.Empty;
    public string MetaKeywords { get; init; } = string.Empty;
}

/// <summary>
/// استودیوی هوش مصنوعی محتوا (سند بخش ۱۴):
/// ادیت عکس، ویدیوی کوتاه، صداپیشگی، ترجمه، سئو.
/// قاعدهٔ سخت: خروجی هرگز با دستکاری رشتهٔ URL ورودی جعل نمی‌شود — همیشه از پاسخ واقعی
/// سرویس AI خوانده می‌شود (و در صورت امکان فایل روی CDN تننت ذخیره می‌شود).
/// Providerها: deepfa (تصویر)، atna (ویدیو)، vira (صدا)، tarjomyar/farazin (ترجمه).
/// </summary>
public interface IAiStudioService
{
    Task<AiStudioResult> EnhancePhotoAsync(EnhancePhotoRequest request, CancellationToken cancellationToken = default);
    Task<AiStudioResult> GenerateVideoStoryAsync(VideoStoryRequest request, CancellationToken cancellationToken = default);
    Task<AiStudioResult> GenerateVoiceOverAsync(VoiceOverRequest request, CancellationToken cancellationToken = default);
    Task<TranslateResult> AutoTranslateAsync(TranslateRequest request, CancellationToken cancellationToken = default);

    /// <summary>تولید متادیتای سئو — محلی و قطعی (بدون API بیرونی).</summary>
    SeoMetaResult GenerateSeoMeta(string productName, string? description);
}
