namespace IGBZ.Application.Pricing;

using IGBZ.Domain.Common;

/// <summary>مرحلهٔ ۱: جمع اقلام → SubTotal.</summary>
public class SubtotalCalculator : IPricingCalculator
{
    public int Order => 10;
    public string Name => "subtotal";

    public Task CalculateAsync(PricingContext context, PricingAccumulator accumulator, CancellationToken cancellationToken)
    {
        var total = Money.Zero;
        foreach (var line in context.Lines)
            total += line.LineTotalToman;

        accumulator.SubTotalToman = total;
        return Task.CompletedTask;
    }
}

/// <summary>مرحلهٔ ۲: تخفیف (کوپن) → DiscountToman + کدهای اعمال‌شده.</summary>
public class DiscountCalculator : IPricingCalculator
{
    private readonly Discounts.IDiscountEngine _discountEngine;

    public DiscountCalculator(Discounts.IDiscountEngine discountEngine)
    {
        _discountEngine = discountEngine;
    }

    public int Order => 20;
    public string Name => "discount";

    public async Task CalculateAsync(PricingContext context, PricingAccumulator accumulator, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(context.CouponCode))
            return;

        var result = await _discountEngine.ApplyCouponAsync(
            context.CouponCode, accumulator.SubTotalToman, context.CustomerId, cancellationToken);

        if (result.IsApplied)
        {
            accumulator.DiscountToman = result.DiscountAmount;
            accumulator.AppliedCouponCodes.Add(result.AppliedCode!);
        }
    }
}

/// <summary>مرحلهٔ ۳: مالیات بر (SubTotal - تخفیف).</summary>
public class TaxCalculator : IPricingCalculator
{
    public int Order => 30;
    public string Name => "tax";

    public Task CalculateAsync(PricingContext context, PricingAccumulator accumulator, CancellationToken cancellationToken)
    {
        var taxable = accumulator.SubTotalToman - accumulator.DiscountToman;
        var rate = Math.Clamp(context.TaxRatePercent, 0, 100);
        accumulator.TaxToman = new Money(decimal.Round(taxable.Toman * rate / 100m, 0, MidpointRounding.AwayFromZero));
        return Task.CompletedTask;
    }
}

/// <summary>مرحلهٔ ۴: هزینهٔ ارسال (از خارج پایپلاین تعیین می‌شود).</summary>
public class ShippingCalculator : IPricingCalculator
{
    public int Order => 40;
    public string Name => "shipping";

    public Task CalculateAsync(PricingContext context, PricingAccumulator accumulator, CancellationToken cancellationToken)
    {
        accumulator.ShippingToman = context.ShippingToman;
        return Task.CompletedTask;
    }
}
