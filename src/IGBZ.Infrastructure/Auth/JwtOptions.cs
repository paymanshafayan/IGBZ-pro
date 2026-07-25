namespace IGBZ.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "IGBZ";
    public string Audience { get; init; } = "IGBZ.Clients";
    public string SigningKey { get; init; } = "IGBZ_DEV_SECRET_CHANGE_ME_1234567890_MIN_32_CHARS";
    public int AccessTokenMinutes { get; init; } = 120;
}
