namespace IGBZ.Domain.Discounts;

/// <summary>
/// کوپن تخفیف سطح ۲ (سند بخش ۸): درصدی یا مبلغ ثابت، حداقل سبد، انقضا، سقف استفاده،
/// عدم ترکیب هم‌زمان دو کد (در موتور اعمال می‌شود).
/// </summary>
public class Discount : Entity, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public DiscountType Type { get; set; } = DiscountType.AssignedToOrderTotal;

    public bool UsePercentage { get; set; }
    public decimal DiscountPercentage { get; set; }

    public decimal? DiscountAmountToman { get; set; }

    public decimal? MinimumOrderAmountToman { get; set; }

    public DateTime? StartDateUtc { get; set; }
    public DateTime? EndDateUtc { get; set; }

    public bool RequiresCouponCode { get; set; }
    public string? CouponCode { get; set; }

    public DiscountLimitationType LimitationType { get; set; } = DiscountLimitationType.Unlimited;
    public int LimitationTimes { get; set; }

    public bool IsActive { get; set; }

    /// <summary>محاسبهٔ مبلغ تخفیف برای یک سبد — بدون بررسی محدودیت‌ها (موتور بررسی می‌کند).</summary>
    public decimal CalculateDiscountAmount(decimal orderTotalToman)
    {
        if (UsePercentage)
        {
            var percent = Math.Clamp(DiscountPercentage, 0, 100);
            return decimal.Round(orderTotalToman * percent / 100m, 0, MidpointRounding.AwayFromZero);
        }

        return Math.Min(DiscountAmountToman ?? 0, orderTotalToman);
    }
}

public enum DiscountType
{
    AssignedToOrderTotal = 1
}

public enum DiscountLimitationType
{
    Unlimited = 0,
    NTimesOnly = 15,
    NTimesPerCustomer = 25
}
