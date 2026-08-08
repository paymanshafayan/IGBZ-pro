namespace IGBZ.Application.Pricing;

using IGBZ.Domain.Common;

/// <summary>خط سفارش برای محاسبهٔ قیمت.</summary>
public class PricingLine
{
    public Money UnitPriceToman { get; init; } = Money.Zero;
    public int Quantity { get; init; }

    public Money LineTotalToman => UnitPriceToman * Quantity;
}

/// <summary>ورودی پایپلاین قیمت‌گذاری.</summary>
public class PricingContext
{
    public IReadOnlyList<PricingLine> Lines { get; init; } = Array.Empty<PricingLine>();
    public decimal TaxRatePercent { get; init; }
    public Money ShippingToman { get; init; } = Money.Zero;
    public string? CouponCode { get; init; }
    public string CustomerId { get; init; } = string.Empty;
}

/// <summary>تجمع‌کنندهٔ مراحل — هر calculator بخشی از آن را پر می‌کند.</summary>
public class PricingAccumulator
{
    public Money SubTotalToman { get; set; } = Money.Zero;
    public Money DiscountToman { get; set; } = Money.Zero;
    public Money TaxToman { get; set; } = Money.Zero;
    public Money ShippingToman { get; set; } = Money.Zero;
    public Money GrandTotalToman { get; set; } = Money.Zero;
    public List<string> AppliedCouponCodes { get; } = new();
}

/// <summary>نتیجهٔ نهایی پایپلاین.</summary>
public class PricingResult
{
    public PricingAccumulator Accumulator { get; init; } = new();
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public bool HasErrors => Errors.Count > 0;
}
