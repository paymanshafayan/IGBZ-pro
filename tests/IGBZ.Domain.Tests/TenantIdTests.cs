namespace IGBZ.Domain.Tests;

using IGBZ.Domain.Tenants;
using Xunit;

public class TenantIdTests
{
    [Fact]
    public void Constructor_NormalizesToLowercase()
    {
        var id = new TenantId("  ModStyle ");
        Assert.Equal("modstyle", id.Value);
    }

    [Fact]
    public void Constructor_Throws_WhenEmpty()
    {
        Assert.Throws<ArgumentException>(() => new TenantId("   "));
    }

    [Fact]
    public void Constructor_Throws_WhenTooLong()
    {
        Assert.Throws<ArgumentException>(() => new TenantId(new string('a', 65)));
    }

    [Fact]
    public void Platform_IsFixedValue()
    {
        Assert.Equal("platform", TenantId.Platform.Value);
    }

    [Fact]
    public void Equality_ByValue()
    {
        Assert.Equal(new TenantId("modstyle"), new TenantId("ModStyle"));
    }
}
