using IGBZ.Domain.Identity;

namespace IGBZ.Application.Auth;

public sealed record RequestOtpRequest(string PhoneNumber, OtpPurpose Purpose, string? DisplayName = null, string? Email = null);

public sealed record RequestOtpResponse(
    string ChallengeId,
    string PhoneNumber,
    OtpPurpose Purpose,
    DateTimeOffset ExpiresAtUtc,
    string? DevelopmentCode);

public sealed record VerifyOtpRequest(string ChallengeId, string PhoneNumber, OtpPurpose Purpose, string Code);

public sealed record AuthenticatedUserResponse(
    string UserId,
    string TenantId,
    string PhoneNumber,
    string? Email,
    string DisplayName,
    IReadOnlyList<UserRole> Roles,
    string AccessToken,
    DateTimeOffset ExpiresAtUtc);
