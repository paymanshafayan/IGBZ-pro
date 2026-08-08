namespace IGBZ.Application.Tests.Fakes;

using IGBZ.Application.Auth;

/// <summary>JWT ساختگی برای تست — بدون نیاز به کلید واقعی.</summary>
public class FakeJwtTokenService : IJwtTokenService
{
    public string GenerateAccessToken(string customerId, string tenantId, bool isTenantOwner, TimeSpan? validFor = null)
        => $"fake-token::{customerId}::{tenantId}::{isTenantOwner}";

    public JwtPayload? ValidateAndDecode(string token)
    {
        var parts = token?.Split("::");
        if (parts is not { Length: 4 } || parts[0] != "fake-token")
            return null;

        return new JwtPayload
        {
            CustomerId = parts[1],
            TenantId = parts[2],
            IsTenantOwner = bool.TryParse(parts[3], out var owner) && owner
        };
    }
}
