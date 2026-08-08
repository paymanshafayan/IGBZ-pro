namespace IGBZ.Application.Tests;

using IGBZ.Application.Discounts;
using IGBZ.Application.Tests.Fakes;
using IGBZ.Domain.Common;
using IGBZ.Domain.Discounts;
using Xunit;

public class DiscountRulesTests
{
    [Fact]
    public async Task ApplyRules_OneRule_AboveThreshold()
    {
        var engine = new DiscountEngine(new FakeDiscountLookup(), new[]
        {
            new PercentageAboveThresholdRule(thresholdToman: 1_000_000, percent: 5, priority: 10)
        });

        var result = await engine.ApplyRulesAsync(new Money(2_000_000), "c1");

        Assert.True(result.IsApplied);
        Assert.Equal(100_000m, result.DiscountAmount.Toman); // 5% از 2M
    }

    [Fact]
    public async Task ApplyRules_BelowThreshold_NotApplied()
    {
        var engine = new DiscountEngine(new FakeDiscountLookup(), new[]
        {
            new PercentageAboveThresholdRule(thresholdToman: 1_000_000, percent: 5, priority: 10)
        });

        var result = await engine.ApplyRulesAsync(new Money(500_000), "c1");

        Assert.False(result.IsApplied);
    }

    [Fact]
    public async Task ApplyRules_MultipleCombinableRules_Sum()
    {
        var engine = new DiscountEngine(new FakeDiscountLookup(), new[]
        {
            new PercentageAboveThresholdRule(thresholdToman: 1_000_000, percent: 5, priority: 10),
            new PercentageAboveThresholdRule(thresholdToman: 500_000, percent: 3, priority: 20)
        });

        var result = await engine.ApplyRulesAsync(new Money(2_000_000), "c1");

        Assert.True(result.IsApplied);
        Assert.Equal(160_000m, result.DiscountAmount.Toman); // 5% + 3% از 2M
    }

    [Fact]
    public async Task ApplyRules_NonCombinable_StopsChain()
    {
        var engine = new DiscountEngine(new FakeDiscountLookup(), new[]
        {
            new PercentageAboveThresholdRule(thresholdToman: 500_000, percent: 5, priority: 10, combinable: false),
            new PercentageAboveThresholdRule(thresholdToman: 500_000, percent: 3, priority: 20)
        });

        var result = await engine.ApplyRulesAsync(new Money(2_000_000), "c1");

        Assert.True(result.IsApplied);
        Assert.Equal(100_000m, result.DiscountAmount.Toman); // فقط 5% (قاعدهٔ 3% چون غیرقابل‌ترکیب بود اجرا نشد)
    }

    [Fact]
    public async Task ApplyRules_DiscountNeverExceedsSubtotal()
    {
        var engine = new DiscountEngine(new FakeDiscountLookup(), new[]
        {
            new PercentageAboveThresholdRule(thresholdToman: 100, percent: 90, priority: 10),
            new PercentageAboveThresholdRule(thresholdToman: 100, percent: 90, priority: 20)
        });

        var result = await engine.ApplyRulesAsync(new Money(1_000), "c1");

        Assert.True(result.IsApplied);
        Assert.True(result.DiscountAmount.Toman <= 1_000);
    }

    [Fact]
    public async Task ApplyRules_NoRules_NotApplied()
    {
        var engine = new DiscountEngine(new FakeDiscountLookup());

        var result = await engine.ApplyRulesAsync(new Money(2_000_000), "c1");

        Assert.False(result.IsApplied);
    }
}
