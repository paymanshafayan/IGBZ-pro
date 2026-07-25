using IGBZ.Domain.Shared;

namespace IGBZ.Domain.Identity;

public sealed class User : TenantScopedEntity
{
    private User()
    {
    }

    private User(string tenantId, string phoneNumber, string? email, string displayName, IReadOnlyCollection<UserRole> roles, DateTimeOffset now)
        : base(tenantId, now)
    {
        PhoneNumber = NormalizePhone(phoneNumber);
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        DisplayName = Guard.AgainstEmpty(displayName, nameof(displayName));
        Roles = roles.Distinct().ToList();
        if (Roles.Count == 0)
        {
            throw new DomainException("User must have at least one role.");
        }

        Status = UserStatus.Active;
    }

    public string PhoneNumber { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public List<UserRole> Roles { get; private set; } = [];
    public UserStatus Status { get; private set; }
    public DateTimeOffset? PhoneVerifiedAtUtc { get; private set; }
    public DateTimeOffset? LastLoginAtUtc { get; private set; }

    public static User CreateTenantOwner(string tenantId, string phoneNumber, string? email, string displayName, DateTimeOffset now)
        => new(tenantId, phoneNumber, email, displayName, [UserRole.TenantOwner, UserRole.TenantAdmin], now);

    public static User CreateCustomer(string tenantId, string phoneNumber, string? email, string displayName, DateTimeOffset now)
        => new(tenantId, phoneNumber, email, displayName, [UserRole.Customer], now);

    public bool HasRole(UserRole role) => Roles.Contains(role);

    public void MarkPhoneVerified(DateTimeOffset now)
    {
        PhoneVerifiedAtUtc = now;
        Touch(now);
    }

    public void MarkLoggedIn(DateTimeOffset now)
    {
        LastLoginAtUtc = now;
        Touch(now);
    }

    public void AddRole(UserRole role, DateTimeOffset now)
    {
        if (!Roles.Contains(role))
        {
            Roles.Add(role);
            Touch(now);
        }
    }

    public static string NormalizePhone(string phoneNumber)
    {
        var normalized = Guard.AgainstEmpty(phoneNumber, nameof(phoneNumber)).Replace(" ", string.Empty).Replace("-", string.Empty);
        if (normalized.StartsWith("+98", StringComparison.Ordinal))
        {
            normalized = "0" + normalized[3..];
        }

        return normalized;
    }
}
