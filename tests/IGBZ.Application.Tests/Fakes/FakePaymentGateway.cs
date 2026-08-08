namespace IGBZ.Application.Tests.Fakes;

using IGBZ.Application.Payments;

/// <summary>درگاه ساختگی برای تست سرویس پرداخت — رفتار قابل‌کنترل.</summary>
public class FakePaymentGateway : IPaymentGateway
{
    public FakePaymentGateway(string gatewayName)
    {
        GatewayName = gatewayName;
    }

    public string GatewayName { get; }

    public bool ShouldFailVerify { get; set; }
    public decimal? ConfirmedAmountToman { get; set; }
    public bool ShouldFailRequest { get; set; }

    public int RequestCallCount { get; private set; }
    public int VerifyCallCount { get; private set; }

    public Task<PaymentGatewayRequestResult> RequestAsync(PaymentGatewayRequest request, CancellationToken cancellationToken = default)
    {
        RequestCallCount++;
        if (ShouldFailRequest)
            return Task.FromResult(new PaymentGatewayRequestResult { IsSuccess = false, ErrorMessage = "درگاه در دسترس نیست." });

        return Task.FromResult(new PaymentGatewayRequestResult
        {
            IsSuccess = true,
            RedirectUrl = $"https://gateway.test/pay/{request.TrackingNumber}"
        });
    }

    public Task<PaymentGatewayVerifyResult> VerifyAsync(PaymentGatewayVerifyRequest request, CancellationToken cancellationToken = default)
    {
        VerifyCallCount++;

        if (ShouldFailVerify)
            return Task.FromResult(new PaymentGatewayVerifyResult { IsSuccess = false, ErrorMessage = "تراکنش ناموفق در درگاه." });

        var amountMatches = !ConfirmedAmountToman.HasValue || ConfirmedAmountToman.Value == request.AmountToman;
        if (!amountMatches)
            return Task.FromResult(new PaymentGatewayVerifyResult { IsSuccess = false, ErrorMessage = "مبلغ تاییدشده مغایرت دارد." });

        return Task.FromResult(new PaymentGatewayVerifyResult
        {
            IsSuccess = true,
            BankRefId = $"BANK-{request.TrackingNumber}"
        });
    }
}
