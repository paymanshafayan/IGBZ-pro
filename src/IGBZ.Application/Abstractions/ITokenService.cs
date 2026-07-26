using IGBZ.Domain.Identity;

namespace IGBZ.Application.Abstractions;

public sealed record TokenResult(string AccessToken, DateTimeOffset ExpiresAtUtc);

public interface ITokenService
{
    TokenResult CreateAccessToken(User user, DateTimeOffset now);
}
