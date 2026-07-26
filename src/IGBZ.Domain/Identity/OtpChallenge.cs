using IGBZ.Domain.Shared;

namespace IGBZ.Domain.Identity;

public sealed class OtpChallenge : TenantScopedEntity
{
    private OtpChallenge()
    {
    }

    private OtpChallenge(string tenantId, string phoneNumber, OtpPurpose purpose, string codeHash, DateTimeOffset expiresAtUtc, DateTimeOffset now)
        : base(tenantId, now)
    {
        PhoneNumber = User.NormalizePhone(phoneNumber);
        Purpose = purpose;
        CodeHash = Guard.AgainstEmpty(codeHash, nameof(codeHash));
        ExpiresAtUtc = expiresAtUtc;
    }

    public string PhoneNumber { get; private set; } = string.Empty;
    public OtpPurpose Purpose { get; private set; }
    public string CodeHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? ConsumedAtUtc { get; private set; }
    public int Attempts { get; private set; }
    public bool IsConsumed => ConsumedAtUtc is not null;

    public static OtpChallenge Create(string tenantId, string phoneNumber, OtpPurpose purpose, string codeHash, TimeSpan ttl, DateTimeOffset now)
        => new(tenantId, phoneNumber, purpose, codeHash, now.Add(ttl), now);

    public bool TryConsume(string providedCodeHash, DateTimeOffset now, int maxAttempts)
    {
        if (IsConsumed || now > ExpiresAtUtc || Attempts >= maxAttempts)
        {
            return false;
        }

        Attempts++;
        if (!string.Equals(CodeHash, providedCodeHash, StringComparison.Ordinal))
        {
            Touch(now);
            return false;
        }

        ConsumedAtUtc = now;
        Touch(now);
        return true;
    }
}
