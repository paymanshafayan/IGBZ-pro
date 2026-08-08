namespace IGBZ.Infrastructure.Lms;

using System.Security.Cryptography;
using System.Text;
using IGBZ.Application.Lms;

/// <summary>
/// توکن امضاشدهٔ HMAC-SHA256 با انقضا و مقید به IP (سند ۱۵.۲):
/// payload = {courseId}.{lessonId}.{customerId}.{ip}.{expiresUnix}
/// امضا با کلید محرمانه (از تنظیمات). اعتبارسنجی با FixedTimeEquals.
/// </summary>
public class LmsVideoSecurityService : ILmsVideoSecurityService
{
    private readonly byte[] _signingKey;

    public LmsVideoSecurityService(string hmacSigningSecret)
    {
        if (string.IsNullOrWhiteSpace(hmacSigningSecret))
            throw new ArgumentException("کلید امضای HMAC ویدیو الزامی است.", nameof(hmacSigningSecret));
        _signingKey = Encoding.UTF8.GetBytes(hmacSigningSecret);
    }

    public Task<SecureVideoUrlResult> GetSecureVideoUrlAsync(
        string courseId, string lessonId, string customerId, string userIpAddress, string? userPhoneNumber,
        TimeSpan? validFor = null, CancellationToken cancellationToken = default)
    {
        var expiresAtUnix = DateTimeOffset.UtcNow.Add(validFor ?? TimeSpan.FromHours(4)).ToUnixTimeSeconds();
        var payload = $"{courseId}.{lessonId}.{customerId}.{userIpAddress}.{expiresAtUnix}";
        var signature = ComputeSignature(payload);
        var secureToken = $"{Base64UrlEncode(payload)}.{signature}";

        var watermark = MaskPhone(userPhoneNumber);
        var embedUrl =
            $"https://vod.arvancloud.ir/embed/{courseId}/{lessonId}" +
            $"?token={Uri.EscapeDataString(secureToken)}" +
            (string.IsNullOrWhiteSpace(watermark) ? string.Empty : $"&wm_text={Uri.EscapeDataString(watermark)}");

        return Task.FromResult(new SecureVideoUrlResult
        {
            IsSuccess = true,
            EmbedPlayerUrl = embedUrl,
            SignedToken = secureToken,
            ExpiresOnUtc = DateTimeOffset.FromUnixTimeSeconds(expiresAtUnix).UtcDateTime
        });
    }

    public bool ValidateSignedToken(string token, string courseId, string lessonId, string customerId, string userIpAddress)
    {
        if (string.IsNullOrWhiteSpace(token) || !token.Contains('.'))
            return false;

        var parts = token.Split('.', 2);
        if (parts.Length != 2)
            return false;

        var payload = Base64UrlDecode(parts[0]);
        if (payload == null)
            return false;

        var providedSignature = parts[1];
        var expectedSignature = ComputeSignature(payload);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(providedSignature),
                Encoding.UTF8.GetBytes(expectedSignature)))
        {
            return false;
        }

        var segments = payload.Split('.');
        if (segments.Length != 5)
            return false;

        var contextMatches =
            segments[0] == courseId
            && segments[1] == lessonId
            && segments[2] == customerId
            && segments[3] == userIpAddress;

        var notExpired = long.TryParse(segments[4], out var expiresAtUnix)
            && DateTimeOffset.UtcNow.ToUnixTimeSeconds() <= expiresAtUnix;

        return contextMatches && notExpired;
    }

    private string ComputeSignature(string payload)
    {
        using var hmac = new HMACSHA256(_signingKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string Base64UrlEncode(string input) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(input)).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static string? Base64UrlDecode(string input)
    {
        try
        {
            var s = input.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            return Encoding.UTF8.GetString(Convert.FromBase64String(s));
        }
        catch
        {
            return null;
        }
    }

    private static string? MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone) || phone.Length < 4)
            return phone;
        return phone[..^4] + "****";
    }
}
