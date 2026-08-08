namespace IGBZ.Application.Tests;

using IGBZ.Application.Discounts;
using IGBZ.Application.Pricing;
using IGBZ.Application.Tests.Fakes;
using IGBZ.Domain.Common;
using Xunit;

public class PricingPipelineTests
{
    private static PricingPipeline BuildPipeline(IDiscountEngine? engine = null)
    {
        var calculators = new List<IPricingCalculator>
        {
            new SubtotalCalculator(),
            new DiscountCalculator(engine ?? new NoopDiscountEngine()),
            new TaxCalculator(),
            new ShippingCalculator()
        };
        return new PricingPipeline(calculators);
    }

    private static PricingContext BuildContext(
        IReadOnlyList<PricingLine> lines,
        decimal taxRate = 9,
        Money? shipping = null,
        string? coupon = null) => new()
    {
        Lines = lines,
        TaxRatePercent = taxRate,
        ShippingToman = shipping ?? Money.Zero,
        CouponCode = coupon,
        CustomerId = "c1"
    };

    [Fact]
    public async Task SimpleBasket_ComputesAllStages()
    {
        var pipeline = BuildPipeline();
        var context = BuildContext(new[]
        {
            new PricingLine { UnitPriceToman = new Money(100_000), Quantity = 2 },
            new PricingLine { UnitPriceToman = new Money(50_000), Quantity = 1 }
        }, taxRate: 10, shipping: new Money(25_000));

        var result = await pipeline.CalculateAsync(context);

        Assert.False(result.HasErrors);
        Assert.Equal(250_000m, result.Accumulator.SubTotalToman.Toman);
        Assert.Equal(0m, result.Accumulator.DiscountToman.Toman);
        Assert.Equal(25_000m, result.Accumulator.TaxToman.Toman);      // 10% از 250k
        Assert.Equal(25_000m, result.Accumulator.ShippingToman.Toman);
        Assert.Equal(300_000m, result.Accumulator.GrandTotalToman.Toman);
    }

    [Fact]
    public async Task WithCoupon_DiscountAppliedBeforeTax()
    {
        var lookup = new FakeDiscountLookup
        {
            Discounts =
            {
                new IGBZ.Domain.Discounts.Discount
                {
                    Id = "d1",
                    TenantId = "t1",
                    Name = "SAVE10",
                    RequiresCouponCode = true,
                    CouponCode = "SAVE10",
                    UsePercentage = true,
                    DiscountPercentage = 10,
                    IsActive = true
                }
            }
        };
        var engine = new DiscountEngine(lookup);
        var pipeline = BuildPipeline(engine);
        var context = BuildContext(new[] { new PricingLine { UnitPriceToman = new Money(100_000), Quantity = 1 } },
            taxRate: 9, coupon: "SAVE10");

        var result = await pipeline.CalculateAsync(context);

        Assert.False(result.HasErrors);
        Assert.Equal(10_000m, result.Accumulator.DiscountToman.Toman);
        Assert.Equal(8_100m, result.Accumulator.TaxToman.Toman);        // 9% از 90k
        Assert.Equal(98_100m, result.Accumulator.GrandTotalToman.Toman);
        Assert.Contains("SAVE10", result.Accumulator.AppliedCouponCodes);
    }

    [Fact]
    public async Task InvalidCoupon_ReturnsError_NoDiscount()
    {
        var engine = new DiscountEngine(new FakeDiscountLookup()); // هیچ کوپنی
        var pipeline = BuildPipeline(engine);
        var context = BuildContext(new[] { new PricingLine { UnitPriceToman = new Money(100_000), Quantity = 1 } },
            coupon: "NOPE");

        var result = await pipeline.CalculateAsync(context);

        Assert.True(result.HasErrors);
        Assert.Equal(0m, result.Accumulator.DiscountToman.Toman);
    }

    [Fact]
    public async Task EmptyBasket_Throws()
    {
        var pipeline = BuildPipeline();
        var context = BuildContext(Array.Empty<PricingLine>());

        await Assert.ThrowsAsync<ArgumentException>(() => pipeline.CalculateAsync(context));
    }

    private sealed class NoopDiscountEngine : IDiscountEngine
    {
        public Task<DiscountApplicationResult> ApplyCouponAsync(string couponCode, Money orderSubtotalToman, string customerId, CancellationToken cancellationToken = default)
            => Task.FromResult(new DiscountApplicationResult { Errors = new[] { "کد نامعتبر" } });
    }
}
