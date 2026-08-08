namespace IGBZ.Infrastructure.Tests;

using System.Net;
using System.Text;
using System.Text.Json;
using IGBZ.Application.Payments;
using IGBZ.Infrastructure.Gateways;
using Xunit;

/// <summary>
/// تست درگاه pay.ir با HttpClient ساختگی (بدون شبکه واقعی).
/// </summary>
public class PayIrGatewayTests
{
    private static (PayIrGateway gateway, FakeHttpMessageHandler handler) Build()
    {
        var handler = new FakeHttpMessageHandler();
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://pay.ir")
        };
        return (new PayIrGateway(client), handler);
    }

    [Fact]
    public async Task Request_Success_ReturnsRedirectUrl()
    {
        var (gateway, handler) = Build();
        handler.RespondJson(200, new { status = 1, token = "tok-123" });

        var result = await gateway.RequestAsync(new PaymentGatewayRequest
        {
            TrackingNumber = "trk-1",
            AmountToman = 100_000,
            CallbackUrl = "https://cb.test",
            Extra = "api-key"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("https://pay.ir/pg/tok-123", result.RedirectUrl);

        // بررسی بدنهٔ ارسالی: مبلغ به ریال
        var sent = handler.LastBody!;
        Assert.Equal(1_000_000L, sent.GetProperty("amount").GetInt64());
        Assert.Equal("api-key", sent.GetProperty("api").GetString());
    }

    [Fact]
    public async Task Request_GatewayRejects_ReturnsError()
    {
        var (gateway, handler) = Build();
        handler.RespondJson(200, new { status = 0, errorMessage = "کلید نامعتبر" });

        var result = await gateway.RequestAsync(new PaymentGatewayRequest
        {
            TrackingNumber = "trk-1",
            AmountToman = 100_000,
            CallbackUrl = "https://cb.test",
            Extra = "bad-key"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("کلید نامعتبر", result.ErrorMessage);
    }

    [Fact]
    public async Task Request_NoApiKey_Fails()
    {
        var (gateway, _) = Build();

        var result = await gateway.RequestAsync(new PaymentGatewayRequest
        {
            TrackingNumber = "trk-1",
            AmountToman = 100_000,
            CallbackUrl = "https://cb.test",
            Extra = null
        });

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Verify_Success_ReturnsBankRefId()
    {
        var (gateway, handler) = Build();
        handler.RespondJson(200, new { status = 1, transId = "T-999", amount = 1_000_000 });

        var result = await gateway.VerifyAsync(new PaymentGatewayVerifyRequest
        {
            TrackingNumber = "tok-123",
            AmountToman = 100_000,
            Extra = "api-key"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("T-999", result.BankRefId);
    }

    [Fact]
    public async Task Verify_Failed_ReturnsError()
    {
        var (gateway, handler) = Build();
        handler.RespondJson(200, new { status = 0, message = "تراکنش یافت نشد" });

        var result = await gateway.VerifyAsync(new PaymentGatewayVerifyRequest
        {
            TrackingNumber = "tok-404",
            AmountToman = 100_000,
            Extra = "api-key"
        });

        Assert.False(result.IsSuccess);
    }
}

/// <summary>HttpClient ساختگی — پاسخ‌های قابل‌کنترل بدون شبکه.</summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private HttpResponseMessage? _response;
    public string? LastBody { get; private set; }

    public void RespondJson(int statusCode, object body)
    {
        _response = new HttpResponseMessage((HttpStatusCode)statusCode)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content != null)
        {
            LastBody = request.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
        }
        return Task.FromResult(_response ?? new HttpResponseMessage(HttpStatusCode.OK));
    }
}
