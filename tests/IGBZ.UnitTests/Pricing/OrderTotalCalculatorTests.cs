using IGBZ.Application.Pricing;
using IGBZ.Domain.Discounts;
using Xunit;

namespace IGBZ.UnitTests.Pricing;

public sealed class OrderTotalCalculatorTests
{
    [Fact]
    public void Calculate_AppliesPercentageCouponBeforeVat()
    {
        var now = DateTimeOffset.UtcNow;
        var calculator = new OrderTotalCalculator();
        var discount = Discount.CreateCoupon("tenant-a", "Ten percent", DiscountType.Percentage, 10, "IGBZ10", now);

        var result = calculator.Calculate(
            new PricingRequest(
                [new PriceLine("p1", "v1", "Product", "Default", "SKU-1", 2, 100_000, "IRR")],
                "IGBZ10",
                20_000,
                0.09m,
                now),
            [discount]);

        Assert.Equal(200_000, result.SubTotal);
        Assert.Equal(20_000, result.DiscountTotal);
        Assert.Equal(16_200, result.TaxTotal);
        Assert.Equal(216_200, result.GrandTotal);
    }

    [Fact]
    public void Calculate_CapsFixedDiscountAtSubtotal()
    {
        var now = DateTimeOffset.UtcNow;
        var calculator = new OrderTotalCalculator();
        var discount = Discount.CreateCoupon("tenant-a", "Huge", DiscountType.FixedAmount, 500_000, "FREE", now);

        var result = calculator.Calculate(
            new PricingRequest(
                [new PriceLine("p1", "v1", "Product", "Default", "SKU-1", 1, 100_000, "IRR")],
                "FREE",
                0,
                0.09m,
                now),
            [discount]);

        Assert.Equal(100_000, result.SubTotal);
        Assert.Equal(100_000, result.DiscountTotal);
        Assert.Equal(0, result.TaxTotal);
        Assert.Equal(0, result.GrandTotal);
    }
}
