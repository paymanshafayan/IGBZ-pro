using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IGBZ.Application.Abstractions;
using IGBZ.Domain.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IGBZ.Infrastructure.Auth;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    public TokenResult CreateAccessToken(User user, DateTimeOffset now)
    {
        var jwt = options.Value;
        var expiresAt = now.AddMinutes(jwt.AccessTokenMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("tenantId", user.TenantId),
            new("phone_number", user.PhoneNumber),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.DisplayName)
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
            claims.Add(new Claim("role", role.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
