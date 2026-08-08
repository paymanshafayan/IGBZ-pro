namespace IGBZ.Infrastructure.Auth;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IGBZ.Application.Auth;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// صدور و اعتبارسنجی JWT — کلید امضا از <see cref="JwtOptions"/> (راز محیطی).
/// ادعاها: customerId, tenantId, isTenantOwner.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly SymmetricSecurityKey _signingKey;

    public JwtTokenService(JwtOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(_options.SigningKey) || _options.SigningKey.Length < 32)
            throw new InvalidOperationException(
                "کلید امضای JWT (Jwt:SigningKey) باید حداقل ۳۲ کاراکتر باشد و از تنظیمات/راز محیطی خوانده شود.");

        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
    }

    public string GenerateAccessToken(string customerId, string tenantId, bool isTenantOwner, TimeSpan? validFor = null)
    {
        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("customerId", customerId),
            new Claim("tenantId", tenantId),
            new Claim("isTenantOwner", isTenantOwner.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expires = DateTime.UtcNow.Add(validFor ?? TimeSpan.FromMinutes(_options.ExpiryMinutes));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public JwtPayload? ValidateAndDecode(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _options.Issuer,
                ValidateAudience = true,
                ValidAudience = _options.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2),
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _signingKey
            };

            var principal = handler.ValidateToken(token, parameters, out _);

            var customerId = principal.FindFirstValue("customerId");
            var tenantId = principal.FindFirstValue("tenantId");
            var isTenantOwner = bool.TryParse(principal.FindFirstValue("isTenantOwner"), out var owner) && owner;

            if (string.IsNullOrWhiteSpace(customerId) || string.IsNullOrWhiteSpace(tenantId))
                return null;

            return new JwtPayload
            {
                CustomerId = customerId,
                TenantId = tenantId,
                IsTenantOwner = isTenantOwner
            };
        }
        catch
        {
            return null;
        }
    }
}
