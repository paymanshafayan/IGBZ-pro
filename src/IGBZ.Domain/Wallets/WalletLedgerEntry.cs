namespace IGBZ.Domain.Wallets;

/// <summary>
/// دفترکل واحد کیف‌پول — موجودی مشتری همیشه SUM(AmountToman) این جدول است (سند بخش ۶.۵).
/// مثبت = واریز، منفی = برداشت. هر تراکنش مالی یک <c>ReferenceCode</c> یکتا برای Idempotency دارد.
/// </summary>
public class WalletLedgerEntry : Entity, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal AmountToman { get; set; }
    public WalletTransactionReason Reason { get; set; }
    public string? ReferenceCode { get; set; }
}

public enum WalletTransactionReason
{
    CashTopUp = 0,
    OrderCashback = 10,
    OrderAiFeatureBonus = 15,
    InstagramDonationReceived = 20,
    ContestReward = 30,
    AffiliateCommissionEarned = 40,
    AffiliateWithdrawalToBank = 45,
    OrderPaymentDebit = 50,
    AiFeatureUsageDebit = 60,
    AiFeatureUsageRefund = 65,
    ManualAdjustment = 100
}
