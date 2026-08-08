namespace IGBZ.Application.Wallets;

using IGBZ.Application.Abstractions;
using IGBZ.Domain.Wallets;

public class WalletService : IWalletService
{
    private readonly ITenantScopedRepository<WalletLedgerEntry> _ledgerRepository;

    public WalletService(ITenantScopedRepository<WalletLedgerEntry> ledgerRepository)
    {
        _ledgerRepository = ledgerRepository;
    }

    public async Task<decimal> GetBalanceAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var entries = await _ledgerRepository.FindAsync(e => e.CustomerId == customerId, cancellationToken);
        return entries.Sum(e => e.AmountToman);
    }

    public async Task<decimal> CreditAsync(
        string customerId,
        decimal amountToman,
        WalletTransactionReason reason,
        string? referenceCode,
        CancellationToken cancellationToken = default)
    {
        if (amountToman <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountToman), "مبلغ واریزی باید مثبت باشد.");

        // Idempotency: اگر همین تراکنش (reason+referenceCode) قبلاً ثبت شده، دوباره واریز نکن
        if (!string.IsNullOrWhiteSpace(referenceCode))
        {
            var existing = await _ledgerRepository.FirstOrDefaultAsync(
                e => e.CustomerId == customerId && e.Reason == reason && e.ReferenceCode == referenceCode,
                cancellationToken);
            if (existing != null)
                return await GetBalanceAsync(customerId, cancellationToken);
        }

        await _ledgerRepository.InsertAsync(new WalletLedgerEntry
        {
            CustomerId = customerId,
            AmountToman = amountToman,
            Reason = reason,
            ReferenceCode = referenceCode,
            CreatedOnUtc = DateTime.UtcNow
        }, cancellationToken);

        return await GetBalanceAsync(customerId, cancellationToken);
    }

    public async Task<WalletDebitResult> TryDebitAsync(
        string customerId,
        decimal amountToman,
        WalletTransactionReason reason,
        string? referenceCode,
        CancellationToken cancellationToken = default)
    {
        if (amountToman <= 0)
            return new WalletDebitResult { Success = true, NewBalanceToman = await GetBalanceAsync(customerId, cancellationToken) };

        // Idempotency: کسر تکراری با همان ReferenceCode دوباره اعمال نمی‌شود
        if (!string.IsNullOrWhiteSpace(referenceCode))
        {
            var existing = await _ledgerRepository.FirstOrDefaultAsync(
                e => e.CustomerId == customerId && e.Reason == reason && e.ReferenceCode == referenceCode,
                cancellationToken);
            if (existing != null)
                return new WalletDebitResult { Success = true, NewBalanceToman = await GetBalanceAsync(customerId, cancellationToken) };
        }

        var balance = await GetBalanceAsync(customerId, cancellationToken);
        if (balance < amountToman)
            return new WalletDebitResult
            {
                Success = false,
                NewBalanceToman = balance,
                ErrorMessage = "موجودی کیف‌پول کافی نیست."
            };

        await _ledgerRepository.InsertAsync(new WalletLedgerEntry
        {
            CustomerId = customerId,
            AmountToman = -amountToman,
            Reason = reason,
            ReferenceCode = referenceCode,
            CreatedOnUtc = DateTime.UtcNow
        }, cancellationToken);

        return new WalletDebitResult
        {
            Success = true,
            NewBalanceToman = await GetBalanceAsync(customerId, cancellationToken)
        };
    }
}
