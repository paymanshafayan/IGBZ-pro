namespace IGBZ.Domain.Plans;

/// <summary>
/// پلن اشتراکی — موجودیت سطح پلتفرم (بدون tenantId؛ مثل جدول tenantPlans در سند بخش ۶.۹).
/// قیمت‌ها فقط از همین جدول خوانده می‌شوند (هرگز Hardcode در کد).
/// </summary>
public class TenantPlan : Entity
{
    public string Name { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>محصول پرداختی مرتبط (اختیاری در فاز ۱ — برای فروش پلن).</summary>
    public string? LinkedProductId { get; set; }

    public int MaxProductsAllowed { get; set; }
    public int MaxOrdersPerMonth { get; set; }

    public bool AllowCustomDomain { get; set; }
    public bool AllowDedicatedApp { get; set; }
    public bool AllowStore { get; set; }
    public bool AllowInstagramAiAssistant { get; set; }
    public bool AllowInstagramAiAssistantPro { get; set; }

    public decimal PriceMonthlyToman { get; set; }
    public decimal PriceSixMonthsToman { get; set; }
    public decimal PriceYearlyToman { get; set; }

    /// <summary>اگر بزرگ‌تر از صفر باشد، پلن آزمایشی رایگان است.</summary>
    public int TrialDurationDays { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }

    public decimal GetPriceToman(BillingCycle cycle) => cycle switch
    {
        BillingCycle.SixMonths => PriceSixMonthsToman,
        BillingCycle.Yearly => PriceYearlyToman,
        _ => PriceMonthlyToman
    };

    public int GetDurationDays(BillingCycle cycle) => cycle switch
    {
        BillingCycle.SixMonths => 183,
        BillingCycle.Yearly => 365,
        _ => 30
    };
}

public enum BillingCycle
{
    Monthly = 0,
    SixMonths = 1,
    Yearly = 2
}
