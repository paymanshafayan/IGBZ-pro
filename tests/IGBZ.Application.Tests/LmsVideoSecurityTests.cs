namespace IGBZ.Application.Tests;

using IGBZ.Infrastructure.Lms;
using Xunit;

public class LmsVideoSecurityTests
{
    private const string Secret = "test-lms-secret-0123456789abcdef";

    [Fact]
    public async Task GetSecureVideoUrl_ReturnsSignedTokenWithExpiry()
    {
        var service = new LmsVideoSecurityService(Secret);

        var result = await service.GetSecureVideoUrlAsync("c1", "l1", "cust1", "1.2.3.4", "09121234567");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.SignedToken);
        Assert.Contains("token=", result.EmbedPlayerUrl);
        Assert.Contains("wm_text=", result.EmbedPlayerUrl); // واترمارک
        Assert.True(result.ExpiresOnUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task ValidateSignedToken_ValidToken_Passes()
    {
        var service = new LmsVideoSecurityService(Secret);
        var result = await service.GetSecureVideoUrlAsync("c1", "l1", "cust1", "1.2.3.4", null);

        var valid = service.ValidateSignedToken(result.SignedToken!, "c1", "l1", "cust1", "1.2.3.4");

        Assert.True(valid);
    }

    [Fact]
    public async Task ValidateSignedToken_WrongIp_Fails()
    {
        var service = new LmsVideoSecurityService(Secret);
        var result = await service.GetSecureVideoUrlAsync("c1", "l1", "cust1", "1.2.3.4", null);

        var valid = service.ValidateSignedToken(result.SignedToken!, "c1", "l1", "cust1", "9.9.9.9");

        Assert.False(valid);
    }

    [Fact]
    public async Task ValidateSignedToken_WrongCustomer_Fails()
    {
        var service = new LmsVideoSecurityService(Secret);
        var result = await service.GetSecureVideoUrlAsync("c1", "l1", "cust1", "1.2.3.4", null);

        var valid = service.ValidateSignedToken(result.SignedToken!, "c1", "l1", "cust2", "1.2.3.4");

        Assert.False(valid);
    }

    [Fact]
    public async Task ValidateSignedToken_TamperedToken_Fails()
    {
        var service = new LmsVideoSecurityService(Secret);
        var result = await service.GetSecureVideoUrlAsync("c1", "l1", "cust1", "1.2.3.4", null);

        var tampered = result.SignedToken! + "x";
        var valid = service.ValidateSignedToken(tampered, "c1", "l1", "cust1", "1.2.3.4");

        Assert.False(valid);
    }

    [Fact]
    public async Task ValidateSignedToken_ExpiredToken_Fails()
    {
        var service = new LmsVideoSecurityService(Secret);
        var result = await service.GetSecureVideoUrlAsync("c1", "l1", "cust1", "1.2.3.4", null, TimeSpan.FromSeconds(-1));

        var valid = service.ValidateSignedToken(result.SignedToken!, "c1", "l1", "cust1", "1.2.3.4");

        Assert.False(valid);
    }

    [Fact]
    public void Constructor_EmptySecret_Throws()
    {
        Assert.Throws<ArgumentException>(() => new LmsVideoSecurityService(""));
    }
}
