namespace IGBZ.Application.Discounts;

using IGBZ.Domain.Common;

public class DiscountApplicationResult
{
    public bool IsApplied { get; init; }
    public Money DiscountAmount { get; init; } = Money.Zero;
    public string? AppliedCode { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}

/// <summary>موتور تخفیف — سطح ۲ (کوپن) + سطح ۳ (قواعد چندگانه با priority/combinable).</summary>
public interface IDiscountEngine
{
    Task<DiscountApplicationResult> ApplyCouponAsync(
        string couponCode,
        Money orderSubtotalToman,
        string customerId,
        CancellationToken cancellationToken = default);

    /// <summary>اعمال همهٔ قواعد سطح ۳ (بر اساس priority و combinable) — جمع تخفیف‌ها.</summary>
    Task<DiscountApplicationResult> ApplyRulesAsync(
        Money orderSubtotalToman,
        string customerId,
        CancellationToken cancellationToken = default);
}
