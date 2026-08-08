namespace IGBZ.Application.Tests;

using IGBZ.Application.BNPL;
using IGBZ.Application.Tenancy;
using IGBZ.Application.Tests.Fakes;
using Xunit;

public class BnplServiceTests
{
    private readonly TenantContext _tenantContext = new();

    private sealed class FakeBnplGateway : IBnplGateway
    {
        public FakeBnplGateway(string providerKey)
        {
            ProviderKey = providerKey;
        }

        public string ProviderKey { get; }

        public bool EligibilityResult { get; set; } = true;
        public bool ShouldFailRequest { get; set; }
        public bool ShouldFailVerify { get; set; }
        public string? LastApiKey { get; private set; }

        public Task<BnplEligibilityResult> CheckEligibilityAsync(
            BnplEligibilityRequest request, string apiKey, CancellationToken cancellationToken = default)
        {
            LastApiKey = apiKey;
            return Task.FromResult(new BnplEligibilityResult { IsEligible = EligibilityResult });
        }

        public Task<BnplPaymentRequestResult> RequestPaymentAsync(
            BnplPaymentRequest request, string apiKey, CancellationToken cancellationToken = default)
        {
            LastApiKey = apiKey;
            if (ShouldFailRequest)
                return Task.FromResult(new BnplPaymentRequestResult { IsSuccess = false, ErrorMessage = "خطا" });
            return Task.FromResult(new BnplPaymentRequestResult
            {
                IsSuccess = true,
                RedirectUrl = $"https://gateway/{request.TransactionId}",
                PaymentToken = "tok"
            });
        }

        public Task<BnplVerifyResult> VerifyPaymentAsync(
            BnplVerifyRequest request, string apiKey, CancellationToken cancellationToken = default)
        {
            LastApiKey = apiKey;
            if (ShouldFailVerify)
                return Task.FromResult(new BnplVerifyResult { IsSuccess = false, ErrorMessage = "خطا" });
            return Task.FromResult(new BnplVerifyResult { IsSuccess = true, TrackingCode = "TC-1" });
        }
    }

    private (BnplService service, FakeBnplGateway gateway, FakeCredentialRepository credentials) Build()
    {
        _tenantContext.Set("t1");
        var credentials = new FakeCredentialRepository(_tenantContext);
        var gateway = new FakeBnplGateway("digipay");
        var service = new BnplService(credentials, new[] { gateway });
        return (service, gateway, credentials);
    }

    [Fact]
    public async Task CheckEligibility_NoCredential_Fails()
    {
        var (service, _, _) = Build();

        var result = await service.CheckEligibilityAsync("digipay", 100_000, "0912", null);

        Assert.False(result.IsEligible);
    }

    [Fact]
    public async Task CheckEligibility_WithCredential_Delegates()
    {
        var (service, gateway, credentials) = Build();
        credentials.AddActiveCredential("digipay", "json-config");

        var result = await service.CheckEligibilityAsync("digipay", 100_000, "0912", null);

        Assert.True(result.IsEligible);
        Assert.Equal("json-config", gateway.LastApiKey);
    }

    [Fact]
    public async Task StartPayment_Success()
    {
        var (service, gateway, credentials) = Build();
        credentials.AddActiveCredential("digipay", "json-config");

        var result = await service.StartPaymentAsync("digipay", "order-1", 100_000, "https://cb", "0912");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.TransactionId);
        Assert.NotNull(result.RedirectUrl);
        Assert.Equal("tok", result.PaymentToken);
    }

    [Fact]
    public async Task StartPayment_UnknownProvider_Fails()
    {
        var (service, _, credentials) = Build();
        credentials.AddActiveCredential("digipay", "json-config");

        var result = await service.StartPaymentAsync("nope", "order-1", 100_000, "https://cb", "0912");

        Assert.False(result.IsSuccess);
        Assert.Contains("پشتیبانی نمی", result.ErrorMessage);
    }

    [Fact]
    public async Task Verify_Success()
    {
        var (service, _, credentials) = Build();
        credentials.AddActiveCredential("digipay", "json-config");

        var result = await service.VerifyPaymentAsync("digipay", "trx-1", "tok");

        Assert.True(result.IsSuccess);
        Assert.Equal("TC-1", result.TrackingCode);
    }
}
