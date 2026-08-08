namespace IGBZ.Application.Tests;

using IGBZ.Application.Discounts;
using IGBZ.Application.Tests.Fakes;
using IGBZ.Domain.Common;
using IGBZ.Domain.Discounts;
using Xunit;

public class DiscountEngineTests
{
    private static Discount ActivePercentCoupon(decimal percent = 10) => new()
    {
        Id = "d1",
        TenantId = "t1",
        Name = "تخفیف",
        RequiresCouponCode = true,
        CouponCode = "SAVE10",
        UsePercentage = true,
        DiscountPercentage = percent,
        IsActive = true
    };

    [Fact]
    public async Task ValidCoupon_Applies()
    {
        var lookup = new FakeDiscountLookup { Discounts = { ActivePercentCoupon() } };
        var engine = new DiscountEngine(lookup);

        var result = await engine.ApplyCouponAsync("SAVE10", new Money(100_000), "c1");

        Assert.True(result.IsApplied);
        Assert.Equal(10_000m, result.DiscountAmount.Toman);
    }

    [Fact]
    public async Task UnknownCode_Fails()
    {
        var engine = new DiscountEngine(new FakeDiscountLookup());
        var result = await engine.ApplyCouponAsync("NOPE", new Money(100_000), "c1");

        Assert.False(result.IsApplied);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ExpiredCoupon_Fails()
    {
        var discount = ActivePercentCoupon();
        discount.EndDateUtc = DateTime.UtcNow.AddDays(-1);

        var engine = new DiscountEngine(new FakeDiscountLookup { Discounts = { discount } });
        var result = await engine.ApplyCouponAsync("SAVE10", new Money(100_000), "c1");

        Assert.False(result.IsApplied);
        Assert.Contains(result.Errors, e => e.Contains("منقضی", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task BelowMinimumOrder_Fails()
    {
        var discount = ActivePercentCoupon();
        discount.MinimumOrderAmountToman = 200_000;

        var engine = new DiscountEngine(new FakeDiscountLookup { Discounts = { discount } });
        var result = await engine.ApplyCouponAsync("SAVE10", new Money(100_000), "c1");

        Assert.False(result.IsApplied);
    }

    [Fact]
    public async Task ExhaustedUsage_Fails()
    {
        var discount = ActivePercentCoupon();
        discount.LimitationType = DiscountLimitationType.NTimesOnly;
        discount.LimitationTimes = 1;

        var lookup = new FakeDiscountLookup { Discounts = { discount } };
        lookup.UsageCounts["d1|c1"] = 1;

        var engine = new DiscountEngine(lookup);
        var result = await engine.ApplyCouponAsync("SAVE10", new Money(100_000), "c1");

        Assert.False(result.IsApplied);
        Assert.Contains(result.Errors, e => e.Contains("سقف استفاده"));
    }

    [Fact]
    public async Task FixedAmountCoupon_Applies()
    {
        var discount = new Discount
        {
            Id = "d2",
            TenantId = "t1",
            Name = "مبلغ ثابت",
            RequiresCouponCode = true,
            CouponCode = "FIX50",
            UsePercentage = false,
            DiscountAmountToman = 50_000,
            IsActive = true
        };

        var engine = new DiscountEngine(new FakeDiscountLookup { Discounts = { discount } });
        var result = await engine.ApplyCouponAsync("FIX50", new Money(100_000), "c1");

        Assert.True(result.IsApplied);
        Assert.Equal(50_000m, result.DiscountAmount.Toman);
    }
}
