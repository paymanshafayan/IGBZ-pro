namespace IGBZ.Domain.Integration;

/// <summary>
/// اعتبارنامهٔ یک سرویس بیرونی برای یک تننت (سند بخش ۶.۶).
/// کلیدها همیشه به‌صورت رمزنگاری‌شده (AES) ذخیره می‌شوند؛ نبود کلیدِ فعال = خطای صریح، نه موفقیت خاموش.
/// </summary>
public class IntegrationCredential : Entity, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;

    /// <summary>کلید Provider — مثلاً "payir" یا "instagram.graph".</summary>
    public string ProviderKey { get; set; } = string.Empty;

    public string? ApiKeyEncrypted { get; set; }
    public string? ApiSecretEncrypted { get; set; }
    public string? EndpointOverrideUrl { get; set; }

    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }

    public DateTime? LastTestedOnUtc { get; set; }
    public string? LastTestResultMessage { get; set; }

    public void MarkVerified(string? message)
    {
        IsVerified = true;
        LastTestedOnUtc = DateTime.UtcNow;
        LastTestResultMessage = message;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public void MarkTestFailed(string? message)
    {
        IsVerified = false;
        LastTestedOnUtc = DateTime.UtcNow;
        LastTestResultMessage = message;
        UpdatedOnUtc = DateTime.UtcNow;
    }
}
