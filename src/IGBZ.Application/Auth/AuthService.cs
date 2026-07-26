using System.Security.Cryptography;
using System.Text;
using IGBZ.Application.Abstractions;
using IGBZ.Domain.Identity;
using IGBZ.Domain.Shared;

namespace IGBZ.Application.Auth;

public sealed class AuthService(
    ITenantContextAccessor tenantContextAccessor,
    ITenantScopedRepository<User> users,
    ITenantScopedRepository<OtpChallenge> challenges,
    ITokenService tokenService,
    OtpOptions otpOptions,
    IClock clock)
{
    public async Task<RequestOtpResponse> RequestOtpAsync(RequestOtpRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.RequiredTenantId;
        var phone = User.NormalizePhone(request.PhoneNumber);
        var now = clock.UtcNow;

        var user = await users.FirstOrDefaultAsync(candidate => candidate.PhoneNumber == phone, cancellationToken);
        if (request.Purpose == OtpPurpose.AdminLogin)
        {
            if (user is null || (!user.HasRole(UserRole.TenantOwner) && !user.HasRole(UserRole.TenantAdmin)))
            {
                throw new DomainException("Admin user was not found for this tenant.");
            }
        }
        else if (request.Purpose == OtpPurpose.CustomerLogin && user is null)
        {
            user = User.CreateCustomer(
                tenantId,
                phone,
                request.Email,
                string.IsNullOrWhiteSpace(request.DisplayName) ? phone : request.DisplayName,
                now);
            await users.AddAsync(user, cancellationToken);
        }

        var code = GenerateOtpCode();
        var challenge = OtpChallenge.Create(tenantId, phone, request.Purpose, HashCode(phone, code), otpOptions.Ttl, now);
        await challenges.AddAsync(challenge, cancellationToken);

        return new RequestOtpResponse(
            challenge.Id,
            phone,
            challenge.Purpose,
            challenge.ExpiresAtUtc,
            otpOptions.IncludeCodeInResponse ? code : null);
    }

    public async Task<AuthenticatedUserResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.RequiredTenantId;
        var phone = User.NormalizePhone(request.PhoneNumber);
        var now = clock.UtcNow;
        var challenge = await challenges.GetByIdAsync(request.ChallengeId, cancellationToken)
            ?? throw new DomainException("OTP challenge was not found.");

        if (!string.Equals(challenge.PhoneNumber, phone, StringComparison.OrdinalIgnoreCase) || challenge.Purpose != request.Purpose)
        {
            throw new DomainException("OTP challenge does not match the verification request.");
        }

        if (!challenge.TryConsume(HashCode(phone, request.Code), now, otpOptions.MaxAttempts))
        {
            await challenges.ReplaceAsync(challenge, cancellationToken);
            throw new DomainException("OTP code is invalid, expired, or already consumed.");
        }

        await challenges.ReplaceAsync(challenge, cancellationToken);

        var user = await users.FirstOrDefaultAsync(candidate => candidate.PhoneNumber == phone, cancellationToken)
            ?? throw new DomainException("User was not found for this tenant.");

        if (request.Purpose == OtpPurpose.AdminLogin && !user.HasRole(UserRole.TenantAdmin) && !user.HasRole(UserRole.TenantOwner))
        {
            throw new DomainException("User is not allowed to access tenant admin area.");
        }

        user.MarkPhoneVerified(now);
        user.MarkLoggedIn(now);
        await users.ReplaceAsync(user, cancellationToken);

        var token = tokenService.CreateAccessToken(user, now);
        return new AuthenticatedUserResponse(
            user.Id,
            tenantId,
            user.PhoneNumber,
            user.Email,
            user.DisplayName,
            user.Roles,
            token.AccessToken,
            token.ExpiresAtUtc);
    }

    private static string GenerateOtpCode()
        => RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

    private static string HashCode(string phoneNumber, string code)
    {
        var payload = $"{User.NormalizePhone(phoneNumber)}:{Guard.AgainstEmpty(code, nameof(code))}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }
}
