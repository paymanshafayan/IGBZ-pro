namespace IGBZ.Domain.Tenants;

/// <summary>
/// شناسهٔ تننت — یک slug کوچک‌شده (مثل "modstyle"). سایت مادر = "platform".
/// </summary>
public sealed class TenantId : ValueObject
{
    public string Value { get; }

    public TenantId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("شناسهٔ تننت نمی‌تواند خالی باشد.", nameof(value));

        var cleaned = value.Trim().ToLowerInvariant();
        if (cleaned.Length > 64)
            throw new ArgumentException("شناسهٔ تننت حداکثر ۶۴ کاراکتر است.", nameof(value));

        Value = cleaned;
    }

    public static TenantId Platform => new("platform");

    public static implicit operator string(TenantId tenantId) => tenantId.Value;

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
