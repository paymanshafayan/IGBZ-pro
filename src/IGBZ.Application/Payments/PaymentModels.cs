namespace IGBZ.Application.Payments;

/// <summary>درخواست پرداخت به یک درگاه.</summary>
public class PaymentGatewayRequest
{
    public string TrackingNumber { get; init; } = string.Empty;
    public decimal AmountToman { get; init; }
    public string CallbackUrl { get; init; } = string.Empty;
    public string? Extra { get; init; }
}

public class PaymentGatewayRequestResult
{
    public bool IsSuccess { get; init; }
    public string? RedirectUrl { get; init; }
    public string? ErrorMessage { get; init; }
}

public class PaymentGatewayVerifyRequest
{
    public string TrackingNumber { get; init; } = string.Empty;
    public decimal AmountToman { get; init; }

    /// <summary>اعتبارنامه/کلید درگاه (از اعتبارنامهٔ تننت) — در Verify هم لازم است.</summary>
    public string? Extra { get; init; }
}

public class PaymentGatewayVerifyResult
{
    public bool IsSuccess { get; init; }
    public string? BankRefId { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// انتزاع یک درگاه پرداخت — پیاده‌سازی‌های واقعی (pay.ir و…) در فاز ۲ اضافه می‌شوند.
/// </summary>
public interface IPaymentGateway
{
    string GatewayName { get; }

    Task<PaymentGatewayRequestResult> RequestAsync(PaymentGatewayRequest request, CancellationToken cancellationToken = default);

    Task<PaymentGatewayVerifyResult> VerifyAsync(PaymentGatewayVerifyRequest request, CancellationToken cancellationToken = default);
}
