namespace IGBZ.Domain.Discounts;

/// <summary>
/// قاعدهٔ تخفیف سطح ۳ (سند بخش ۸): چند قاعده با priority و combinable.
/// پایپلاین از List&lt;AppliedDiscount&gt; استفاده می‌کند (نه مقدار تکی) — از قبل آماده بود.
/// </summary>
public interface IDiscountRule
{
    string RuleKey { get; }

    /// <summary>ترتیب اجرا — کوچک‌تر زودتر.</summary>
    int Priority { get; }

    /// <summary>آیا این قاعده با قواعد دیگر قابل ترکیب است؟</summary>
    bool Combinable { get; }

    DiscountRuleResult Apply(DiscountRuleContext context);
}

public class DiscountRuleContext
{
    public decimal OrderSubtotalToman { get; init; }
    public string CustomerId { get; init; } = string.Empty;
    public Dictionary<string, object?> Extra { get; init; } = new();
}

public class DiscountRuleResult
{
    public bool IsApplied { get; init; }
    public decimal DiscountToman { get; init; }
    public string? AppliedRuleKey { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>نمونهٔ قاعده: تخفیف درصدی روی مبلغ بالای آستانه (قابل ترکیب).</summary>
public class PercentageAboveThresholdRule : IDiscountRule
{
    public PercentageAboveThresholdRule(decimal thresholdToman, decimal percent, int priority = 10, bool combinable = true)
    {
        ThresholdToman = thresholdToman;
        Percent = percent;
        Priority = priority;
        Combinable = combinable;
    }

    public decimal ThresholdToman { get; }
    public decimal Percent { get; }
    public string RuleKey => $"percent-above-{ThresholdToman}";
    public int Priority { get; }
    public bool Combinable { get; }

    public DiscountRuleResult Apply(DiscountRuleContext context)
    {
        if (context.OrderSubtotalToman < ThresholdToman)
            return new DiscountRuleResult { IsApplied = false, AppliedRuleKey = RuleKey, ErrorMessage = "زیر آستانه" };

        var discount = decimal.Round(context.OrderSubtotalToman * Percent / 100m, 0, MidpointRounding.AwayFromZero);
        return new DiscountRuleResult { IsApplied = true, DiscountToman = discount, AppliedRuleKey = RuleKey };
    }
}
