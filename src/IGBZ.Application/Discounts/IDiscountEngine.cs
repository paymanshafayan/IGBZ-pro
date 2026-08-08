namespace IGBZ.Application.Discounts;

using IGBZ.Domain.Common;

public class DiscountApplicationResult
{
    public bool IsApplied { get; init; }
    public Money DiscountAmount { get; init; } = Money.Zero;
    public string? AppliedCode { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}

/// <summary>موتور تخفیف سطح ۲ — اعتبارسنجی و اعمال یک کوپن روی مبلغ سبد.</summary>
public interface IDiscountEngine
{
    Task<DiscountApplicationResult> ApplyCouponAsync(
        string couponCode,
        Money orderSubtotalToman,
        string customerId,
        CancellationToken cancellationToken = default);
}
