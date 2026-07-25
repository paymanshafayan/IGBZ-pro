using IGBZ.Domain.Shared;

namespace IGBZ.Domain.Discounts;

public sealed class Discount : TenantScopedEntity
{
    private Discount()
    {
    }

    private Discount(
        string tenantId,
        string name,
        DiscountType type,
        decimal value,
        string? couponCode,
        DateTimeOffset? startsAtUtc,
        DateTimeOffset? endsAtUtc,
        DateTimeOffset now)
        : base(tenantId, now)
    {
        Name = Guard.AgainstEmpty(name, nameof(name));
        Type = type;
        Value = Guard.AgainstNegative(value, nameof(value));
        if (type == DiscountType.Percentage && value > 100)
        {
            throw new DomainException("Percentage discount cannot be greater than 100.");
        }

        CouponCode = string.IsNullOrWhiteSpace(couponCode) ? null : couponCode.Trim().ToUpperInvariant();
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;
    public DiscountType Type { get; private set; }
    public decimal Value { get; private set; }
    public string? CouponCode { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset? StartsAtUtc { get; private set; }
    public DateTimeOffset? EndsAtUtc { get; private set; }

    public static Discount CreateCoupon(
        string tenantId,
        string name,
        DiscountType type,
        decimal value,
        string couponCode,
        DateTimeOffset now,
        DateTimeOffset? startsAtUtc = null,
        DateTimeOffset? endsAtUtc = null)
        => new(tenantId, name, type, value, couponCode, startsAtUtc, endsAtUtc, now);

    public static Discount CreateAutomatic(
        string tenantId,
        string name,
        DiscountType type,
        decimal value,
        DateTimeOffset now,
        DateTimeOffset? startsAtUtc = null,
        DateTimeOffset? endsAtUtc = null)
        => new(tenantId, name, type, value, null, startsAtUtc, endsAtUtc, now);

    public bool CanApply(string? couponCode, DateTimeOffset now)
    {
        if (!IsActive)
        {
            return false;
        }

        if (StartsAtUtc is not null && now < StartsAtUtc.Value)
        {
            return false;
        }

        if (EndsAtUtc is not null && now > EndsAtUtc.Value)
        {
            return false;
        }

        if (CouponCode is null)
        {
            return string.IsNullOrWhiteSpace(couponCode);
        }

        return string.Equals(CouponCode, couponCode?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public decimal Calculate(decimal subtotal)
    {
        Guard.AgainstNegative(subtotal, nameof(subtotal));
        var amount = Type switch
        {
            DiscountType.Percentage => subtotal * Value / 100m,
            DiscountType.FixedAmount => Value,
            _ => 0m
        };

        return Math.Min(subtotal, Math.Round(amount, 2, MidpointRounding.AwayFromZero));
    }
}
