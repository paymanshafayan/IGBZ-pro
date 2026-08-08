namespace IGBZ.Application.Wallets;

using IGBZ.Domain.Wallets;

public class WalletDebitResult
{
    public bool Success { get; init; }
    public decimal NewBalanceToman { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// کیف‌پول واحد — موجودی همیشه SUM دفترکل است (سند بخش ۶.۵).
/// کسر با ReferenceCode یکتا Idempotent است (کلیک دوبل/Retry دوبار کسر نمی‌کند).
/// </summary>
public interface IWalletService
{
    Task<decimal> GetBalanceAsync(string customerId, CancellationToken cancellationToken = default);

    Task<decimal> CreditAsync(
        string customerId,
        decimal amountToman,
        WalletTransactionReason reason,
        string? referenceCode,
        CancellationToken cancellationToken = default);

    Task<WalletDebitResult> TryDebitAsync(
        string customerId,
        decimal amountToman,
        WalletTransactionReason reason,
        string? referenceCode,
        CancellationToken cancellationToken = default);
}
