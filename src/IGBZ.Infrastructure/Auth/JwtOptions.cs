namespace IGBZ.Infrastructure.Auth;

/// <summary>تنظیمات JWT — کلید امضا از تنظیمات/راز محیطی خوانده می‌شود، هرگز Hardcode.</summary>
public class JwtOptions
{
    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "igbz";
    public string Audience { get; set; } = "igbz-apps";
    public int ExpiryMinutes { get; set; } = 60 * 24 * 7; // ۷ روز
}
