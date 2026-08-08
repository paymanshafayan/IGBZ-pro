namespace IGBZ.Application.Lms;

/// <summary>نتیجهٔ صدور لینک امن پخش ویدیو.</summary>
public class SecureVideoUrlResult
{
    public bool IsSuccess { get; init; }
    public string? EmbedPlayerUrl { get; init; }
    public string? SignedToken { get; init; }
    public DateTime ExpiresOnUtc { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// امنیت ویدیوی LMS (سند ۱۵.۲): توکن امضاشدهٔ HMAC-SHA256 با انقضا و مقید به IP + شناسهٔ
/// دوره/درس/مشتری؛ واترمارک متحرک شمارهٔ موبایل ماسک‌شده. جعل توکن بدون کلید امضا ممکن نیست.
/// </summary>
public interface ILmsVideoSecurityService
{
    Task<SecureVideoUrlResult> GetSecureVideoUrlAsync(
        string courseId, string lessonId, string customerId, string userIpAddress, string? userPhoneNumber,
        TimeSpan? validFor = null, CancellationToken cancellationToken = default);

    /// <summary>اعتبارسنجی توکن پیش از پخش (باید در Middleware/Controller سرویس VOD فراخوانی شود).</summary>
    bool ValidateSignedToken(string token, string courseId, string lessonId, string customerId, string userIpAddress);
}
