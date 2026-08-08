namespace IGBZ.Application.Tests;

using IGBZ.Application.Tenancy;
using IGBZ.Application.Tests.Fakes;
using IGBZ.Application.Wallets;
using IGBZ.Domain.Wallets;
using Xunit;

public class WalletServiceTests
{
    private readonly TenantContext _tenantContext = new();

    private (WalletService service, FakeTenantScopedRepository<WalletLedgerEntry> repo) Build()
    {
        _tenantContext.Set("t1");
        var repo = new FakeTenantScopedRepository<WalletLedgerEntry>(_tenantContext);
        return (new WalletService(repo), repo);
    }

    [Fact]
    public async Task CreditAndBalance_AreLedgerSum()
    {
        var (service, _) = Build();

        await service.CreditAsync("c1", 100_000, WalletTransactionReason.CashTopUp, "ref-1");
        await service.CreditAsync("c1", 50_000, WalletTransactionReason.ContestReward, "ref-2");
        await service.DebitForTestAsync("c1", 30_000, WalletTransactionReason.OrderPaymentDebit, "ref-3");

        Assert.Equal(120_000m, await service.GetBalanceAsync("c1"));
    }

    [Fact]
    public async Task TryDebit_WithInsufficientBalance_Fails()
    {
        var (service, _) = Build();
        await service.CreditAsync("c1", 10_000, WalletTransactionReason.CashTopUp, "ref-1");

        var result = await service.TryDebitAsync("c1", 20_000, WalletTransactionReason.OrderPaymentDebit, "ref-2");

        Assert.False(result.Success);
        Assert.Contains("کافی", result.ErrorMessage);
        Assert.Equal(10_000m, await service.GetBalanceAsync("c1"));
    }

    [Fact]
    public async Task TryDebit_IsIdempotent_ByReferenceCode()
    {
        var (service, _) = Build();
        await service.CreditAsync("c1", 50_000, WalletTransactionReason.CashTopUp, "ref-topup");

        var first = await service.TryDebitAsync("c1", 30_000, WalletTransactionReason.OrderPaymentDebit, "order-1");
        var second = await service.TryDebitAsync("c1", 30_000, WalletTransactionReason.OrderPaymentDebit, "order-1");

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(20_000m, await service.GetBalanceAsync("c1")); // فقط یک بار کسر شد
    }

    [Fact]
    public async Task Credit_IsIdempotent_ByReferenceCode()
    {
        var (service, repo) = Build();

        await service.CreditAsync("c1", 10_000, WalletTransactionReason.OrderCashback, "cb-1");
        await service.CreditAsync("c1", 10_000, WalletTransactionReason.OrderCashback, "cb-1");

        Assert.Single(repo.Store.Where(e => e.Reason == WalletTransactionReason.OrderCashback));
        Assert.Equal(10_000m, await service.GetBalanceAsync("c1"));
    }

    [Fact]
    public async Task Balance_IsTenantScoped()
    {
        var (service, _) = Build();
        await service.CreditAsync("c1", 10_000, WalletTransactionReason.CashTopUp, "ref-1");

        // تننت دیگری — موجودی باید صفر باشد (همان مشتری ولی تننت متفاوت)
        _tenantContext.Set("t2");
        Assert.Equal(0m, await service.GetBalanceAsync("c1"));
    }
}

internal static class WalletServiceTestExtensions
{
    public static Task<WalletDebitResult> DebitForTestAsync(
        this IWalletService service,
        string customerId,
        decimal amount,
        WalletTransactionReason reason,
        string? referenceCode)
        => service.TryDebitAsync(customerId, amount, reason, referenceCode);
}
