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
    private readonly IReadOnlyList<IDiscountRule> _rules;

    public DiscountEngine(IDiscountLookup lookup, IEnumerable<IDiscountRule>? rules = null)
    {
        _lookup = lookup;
        _rules = (rules ?? Enumerable.Empty<IDiscountRule>())
            .OrderBy(r => r.Priority)
            .ToList();
    }

    /// <summary>
    /// اعمال قواعد سطح ۳: بر اساس priority اجرا می‌شوند؛ قاعده‌های غیرقابل‌ترکیب،
    /// قواعد بعدی را متوقف می‌کنند. جمع تخفیف‌ها هرگز از مبلغ سبد بیشتر نمی‌شود.
    /// </summary>
    public async Task<DiscountApplicationResult> ApplyRulesAsync(
        Money orderSubtotalToman, string customerId, CancellationToken cancellationToken = default)
    {
        if (_rules.Count == 0)
            return new DiscountApplicationResult();

        var context = new DiscountRuleContext
        {
            OrderSubtotalToman = orderSubtotalToman.Toman,
            CustomerId = customerId
        };

        var totalDiscount = 0m;
        var appliedKeys = new List<string>();
        var errors = new List<string>();
        var combinableChain = true;

        foreach (var rule in _rules)
        {
            // قاعدهٔ غیرقابل‌ترکیب → بعد از اولین اعمال، بقیه متوقف می‌شوند
            if (!combinableChain && !rule.Combinable)
                continue;

            var result = rule.Apply(context);
            if (result.IsApplied)
            {
                totalDiscount += result.DiscountToman;
                appliedKeys.Add(result.AppliedRuleKey ?? rule.RuleKey);

                if (!rule.Combinable)
                    combinableChain = false;
            }
            else if (result.ErrorMessage != null)
            {
                errors.Add($"{rule.RuleKey}: {result.ErrorMessage}");
            }
        }

        totalDiscount = Math.Min(totalDiscount, orderSubtotalToman.Toman);

        if (totalDiscount <= 0)
            return new DiscountApplicationResult { Errors = errors.Count > 0 ? errors : new[] { "هیچ قاعده‌ای اعمال نشد." } };

        return new DiscountApplicationResult
        {
            IsApplied = true,
            DiscountAmount = new Money(totalDiscount),
            AppliedCode = string.Join("+", appliedKeys)
        };
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
