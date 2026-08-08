namespace IGBZ.Application.Auth;

/// <summary>صدور و اعتبارسنجی توکن JWT — پیاده‌سازی در لایهٔ زیرساخت.</summary>
public interface IJwtTokenService
{
    string GenerateAccessToken(string customerId, string tenantId, bool isTenantOwner, TimeSpan? validFor = null);

    /// <summary>اعتبارسنجی توکن و استخراج ادعاها — در صورت نامعتبر بودن null.</summary>
    JwtPayload? ValidateAndDecode(string token);
}

public class JwtPayload
{
    public string CustomerId { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public bool IsTenantOwner { get; init; }
}
