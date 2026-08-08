namespace IGBZ.Infrastructure.Gateways;

using IGBZ.Application.Payments;

/// <summary>
/// درگاه تست (برای فاز ۲ و توسعهٔ محلی) — رفتار قطعی و بدون شبکه:
/// Request همیشه موفق با URL هدایت ساختگی؛ Verify همیشه موفق.
/// ⚠️ در Production هرگز استفاده نشود — جایگزین با درگاه واقعی (pay.ir و…) در فاز ۶.
/// </summary>
public class TestPaymentGateway : IPaymentGateway
{
    public string GatewayName => "test";

    public Task<PaymentGatewayRequestResult> RequestAsync(PaymentGatewayRequest request, CancellationToken cancellationToken = default)
    {
        if (request.AmountToman <= 0)
            return Task.FromResult(new PaymentGatewayRequestResult { IsSuccess = false, ErrorMessage = "مبلغ نامعتبر است." });

        return Task.FromResult(new PaymentGatewayRequestResult
        {
            IsSuccess = true,
            RedirectUrl = $"https://gateway.test/pay/{Uri.EscapeDataString(request.TrackingNumber)}"
        });
    }

    public Task<PaymentGatewayVerifyResult> VerifyAsync(PaymentGatewayVerifyRequest request, CancellationToken cancellationToken = default)
    {
        if (request.AmountToman <= 0)
            return Task.FromResult(new PaymentGatewayVerifyResult { IsSuccess = false, ErrorMessage = "مبلغ نامعتبر است." });

        return Task.FromResult(new PaymentGatewayVerifyResult
        {
            IsSuccess = true,
            BankRefId = $"TEST-REF-{request.TrackingNumber}"
        });
    }
}
