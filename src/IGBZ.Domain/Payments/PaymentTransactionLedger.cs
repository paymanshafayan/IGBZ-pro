namespace IGBZ.Domain.Payments;

/// <summary>
/// دفترکل تراکنش‌های پرداخت — هر درخواست/تایید یک رکورد. <c>TrackingNumber</c> یکتا است
/// (Unique Index در دیتابیس) و تایید دوم با state=VerifiedSuccess هرگز دوباره مبلغ را اعتبار نمی‌زند
/// (سند بخش ۶.۴ و ۹.۲ — ضد Replay).
/// </summary>
public class PaymentTransactionLedger : Entity, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string GatewayName { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public decimal AmountToman { get; set; }
    public PaymentTransactionState State { get; set; } = PaymentTransactionState.Requested;
    public string? BankRefId { get; set; }
    public string? RawGatewayResponse { get; set; }
    public DateTime RequestedOnUtc { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedOnUtc { get; set; }
}

public enum PaymentTransactionState
{
    Requested = 0,
    RedirectedToBank = 10,
    VerifiedSuccess = 20,
    VerifiedFailed = 30
}
