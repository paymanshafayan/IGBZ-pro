namespace IGBZ.Application.Auth;

public sealed class OtpOptions
{
    public TimeSpan Ttl { get; init; } = TimeSpan.FromMinutes(5);
    public int MaxAttempts { get; init; } = 5;

    /// <summary>
    /// Development helper: returns OTP code in API response. Production SMS provider should disable this.
    /// </summary>
    public bool IncludeCodeInResponse { get; init; } = true;
}
