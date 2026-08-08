namespace IGBZ.Domain.Tests;

using IGBZ.Domain.Common;
using Xunit;

public class MoneyTests
{
    [Fact]
    public void Constructor_RoundsToNearestToman()
    {
        var money = new Money(1000.55m);
        Assert.Equal(1001m, money.Toman);
    }

    [Fact]
    public void Constructor_Throws_WhenNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-1));
    }

    [Fact]
    public void Add_ReturnsSum()
    {
        var result = new Money(1000) + new Money(2500);
        Assert.Equal(3500m, result.Toman);
    }

    [Fact]
    public void Subtract_ReturnsDifference()
    {
        var result = new Money(3000) - new Money(1000);
        Assert.Equal(2000m, result.Toman);
    }

    [Fact]
    public void Subtract_Throws_WhenNegativeResult()
    {
        Assert.Throws<InvalidOperationException>(() => new Money(500) - new Money(1000));
    }

    [Fact]
    public void Multiply_ByFactor()
    {
        var result = new Money(1000) * 2.5m;
        Assert.Equal(2500m, result.Toman);
    }

    [Fact]
    public void Equality_ByValue()
    {
        Assert.Equal(new Money(500), new Money(500));
        Assert.NotEqual(new Money(500), new Money(501));
    }

    [Fact]
    public void Zero_IsZero()
    {
        Assert.True(Money.Zero.IsZero);
    }
}
