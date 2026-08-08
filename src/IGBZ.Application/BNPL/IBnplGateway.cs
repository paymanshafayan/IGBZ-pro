namespace IGBZ.Application.BNPL;

/// <summary>بررسی اجازهٔ خرید اعتباری/اقساطی مشتری.</summary>
public class BnplEligibilityRequest
{
    public decimal AmountToman { get; init; }
    public string CustomerMobile { get; init; } = string.Empty;
    public string? CustomerNationalId { get; init; }
}

public class BnplEligibilityResult
{
    public bool IsEligible { get; init; }
    public string? Message { get; init; }
}

/// <summary>شروع پرداخت BNPL — بازگشت لینک درگاه + شناسهٔ تراکنش.</summary>
public class BnplPaymentRequest
{
    public string TransactionId { get; init; } = string.Empty;
    public decimal AmountToman { get; init; }
    public string CallbackUrl { get; init; } = string.Empty;
    public string CustomerMobile { get; init; } = string.Empty;
}

public class BnplPaymentRequestResult
{
    public bool IsSuccess { get; init; }
    public string? RedirectUrl { get; init; }
    public string? PaymentToken { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>تایید پرداخت BNPL.</summary>
public class BnplVerifyRequest
{
    public string TransactionId { get; init; } = string.Empty;
    public string PaymentToken { get; init; } = string.Empty;
}

public class BnplVerifyResult
{
    public bool IsSuccess { get; init; }
    public string? TrackingCode { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// انتزاع درگاه BNPL (خرید اعتباری/اقساطی) — پیاده‌سازی‌ها: دیجی‌پی، اسنپ‌پی.
/// قاعدهٔ سخت: بدون فراخوانی واقعی، موفق اعلام نمی‌شود.
/// </summary>
public interface IBnplGateway
{
    string ProviderKey { get; }

    Task<BnplEligibilityResult> CheckEligibilityAsync(
        BnplEligibilityRequest request, string apiKey, CancellationToken cancellationToken = default);

    Task<BnplPaymentRequestResult> RequestPaymentAsync(
        BnplPaymentRequest request, string apiKey, CancellationToken cancellationToken = default);

    Task<BnplVerifyResult> VerifyPaymentAsync(
        BnplVerifyRequest request, string apiKey, CancellationToken cancellationToken = default);
}
