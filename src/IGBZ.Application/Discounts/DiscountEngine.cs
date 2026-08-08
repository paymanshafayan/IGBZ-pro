namespace IGBZ.Application.Discounts;

using IGBZ.Domain.Common;
using IGBZ.Domain.Discounts;

/// <summary>
/// اعتبارسنجی کوپن (سطح ۲ سند بخش ۸): فعال بودن، بازهٔ زمانی، حداقل سبد، سقف استفاده.
/// عدم ترکیب هم‌زمان دو کد توسط پایپلاین تضمین می‌شود (فقط یک CouponCode می‌پذیرد).
/// </summary>
public class DiscountEngine : IDiscountEngine
{
    private readonly IDiscountLookup _lookup;

    public DiscountEngine(IDiscountLookup lookup)
    {
        _lookup = lookup;
    }

    public async Task<DiscountApplicationResult> ApplyCouponAsync(
        string couponCode,
        Money orderSubtotalToman,
        string customerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
            return new DiscountApplicationResult { Errors = new[] { "کد تخفیف وارد نشده است." } };

        var discount = await _lookup.GetActiveByCodeAsync(couponCode.Trim(), cancellationToken);
        if (discount == null)
            return new DiscountApplicationResult { Errors = new[] { "کد تخفیف نامعتبر است." } };

        var now = DateTime.UtcNow;

        if (!discount.IsActive)
            return new DiscountApplicationResult { Errors = new[] { "این کد تخفیف غیرفعال است." } };

        if (discount.StartDateUtc.HasValue && discount.StartDateUtc.Value > now)
            return new DiscountApplicationResult { Errors = new[] { "این کد تخفیف هنوز فعال نشده است." } };

        if (discount.EndDateUtc.HasValue && discount.EndDateUtc.Value < now)
            return new DiscountApplicationResult { Errors = new[] { "این کد تخفیف منقضی شده است." } };

        if (discount.MinimumOrderAmountToman.HasValue && orderSubtotalToman.Toman < discount.MinimumOrderAmountToman.Value)
            return new DiscountApplicationResult
            {
                Errors = new[] { $"حداقل مبلغ سبد برای این کد، {discount.MinimumOrderAmountToman.Value:N0} تومان است." }
            };

        if (discount.LimitationType != DiscountLimitationType.Unlimited)
        {
            var usage = await _lookup.GetUsageCountAsync(discount.Id, customerId, cancellationToken);
            if (discount.LimitationType == DiscountLimitationType.NTimesOnly && usage >= discount.LimitationTimes)
                return new DiscountApplicationResult { Errors = new[] { "سقف استفادهٔ این کد تخفیف به پایان رسیده است." } };

            if (discount.LimitationType == DiscountLimitationType.NTimesPerCustomer && usage >= discount.LimitationTimes)
                return new DiscountApplicationResult { Errors = new[] { "شما حداکثر استفاده از این کد را داشته‌اید." } };
        }

        var amount = new Money(discount.CalculateDiscountAmount(orderSubtotalToman.Toman));
        return new DiscountApplicationResult
        {
            IsApplied = true,
            DiscountAmount = amount,
            AppliedCode = discount.CouponCode ?? discount.Id
        };
    }
}
