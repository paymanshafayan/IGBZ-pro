namespace IGBZ.Application.Payments;

using IGBZ.Domain.Common;

public class PaymentRequestResult
{
    public bool IsSuccess { get; init; }
    public string? TrackingNumber { get; init; }
    public string? RedirectUrl { get; init; }
    public string? ErrorMessage { get; init; }
}

public class PaymentVerifyResult
{
    public bool IsSuccess { get; init; }
    public bool AlreadyProcessed { get; init; }
    public string? BankRefId { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// سرویس پرداخت با دفترکل ضد-Replay (سند بخش ۹.۲):
/// Verify فقط با (۱) فراخوانی واقعی درگاه، (۲) پاسخ موفق، (۳) تطابق مبلغ — موفق است.
/// </summary>
public interface IPaymentService
{
    Task<PaymentRequestResult> RequestAsync(
        string orderId,
        string gatewayName,
        Money amountToman,
        string callbackUrl,
        CancellationToken cancellationToken = default);

    Task<PaymentVerifyResult> VerifyAsync(
        string trackingNumber,
        Money expectedAmountToman,
        CancellationToken cancellationToken = default);
}
