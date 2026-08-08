namespace IGBZ.Application.Payments;

using IGBZ.Application.Abstractions;
using IGBZ.Application.Tenancy;
using IGBZ.Domain.Common;
using IGBZ.Domain.Integration;
using IGBZ.Domain.Payments;

public class PaymentService : IPaymentService
{
    private readonly ITenantScopedRepository<PaymentTransactionLedger> _ledgerRepository;
    private readonly ITenantScopedRepository<IntegrationCredential> _credentialRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IReadOnlyDictionary<string, IPaymentGateway> _gateways;

    public PaymentService(
        ITenantScopedRepository<PaymentTransactionLedger> ledgerRepository,
        ITenantScopedRepository<IntegrationCredential> credentialRepository,
        IEncryptionService encryptionService,
        IEnumerable<IPaymentGateway> gateways)
    {
        _ledgerRepository = ledgerRepository;
        _credentialRepository = credentialRepository;
        _encryptionService = encryptionService;
        _gateways = gateways.ToDictionary(g => g.GatewayName, g => g, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>خواندن کلید API فعال درگاه برای تننت جاری — رمزگشایی AES (سند بخش ۱۶).</summary>
    private async Task<string?> GetGatewayApiKeyAsync(string gatewayName, CancellationToken cancellationToken)
    {
        var credential = await _credentialRepository.FirstOrDefaultAsync(
            c => c.ProviderKey == gatewayName && c.IsActive, cancellationToken);
        return credential == null ? null : _encryptionService.Decrypt(credential.ApiKeyEncrypted ?? string.Empty);
    }

    public async Task<PaymentRequestResult> RequestAsync(
        string orderId,
        string gatewayName,
        Money amountToman,
        string callbackUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            throw new ArgumentException("شناسهٔ سفارش الزامی است.", nameof(orderId));

        if (!_gateways.TryGetValue(gatewayName, out var gateway))
            return new PaymentRequestResult { IsSuccess = false, ErrorMessage = $"درگاه «{gatewayName}» پشتیبانی نمی‌شود." };

        // کلید API درگاه از اعتبارنامهٔ تننت — نبود کلید = خطای صریح (سند بخش ۶.۶)
        var apiKey = await GetGatewayApiKeyAsync(gatewayName, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
            return new PaymentRequestResult
            {
                IsSuccess = false,
                ErrorMessage = $"کلید API فعالی برای درگاه «{gatewayName}» در این فروشگاه ثبت نشده است."
            };

        var rawTracking = $"{gatewayName}-{orderId}-{Guid.NewGuid():N}";
        var trackingNumber = rawTracking.Length > 60 ? rawTracking[..60] : rawTracking;

        // ثبت درخواست در دفترکل
        var ledger = new PaymentTransactionLedger
        {
            OrderId = orderId,
            GatewayName = gateway.GatewayName,
            TrackingNumber = trackingNumber,
            AmountToman = amountToman.Toman,
            State = PaymentTransactionState.Requested,
            RequestedOnUtc = DateTime.UtcNow
        };
        await _ledgerRepository.InsertAsync(ledger, cancellationToken);

        var gatewayResult = await gateway.RequestAsync(new PaymentGatewayRequest
        {
            TrackingNumber = trackingNumber,
            AmountToman = amountToman.Toman,
            CallbackUrl = callbackUrl,
            Extra = apiKey
        }, cancellationToken);

        if (!gatewayResult.IsSuccess)
        {
            ledger.State = PaymentTransactionState.VerifiedFailed;
            ledger.RawGatewayResponse = gatewayResult.ErrorMessage;
            ledger.VerifiedOnUtc = DateTime.UtcNow;
            await _ledgerRepository.UpdateAsync(ledger, cancellationToken);

            return new PaymentRequestResult
            {
                IsSuccess = false,
                TrackingNumber = trackingNumber,
                ErrorMessage = gatewayResult.ErrorMessage
            };
        }

        ledger.State = PaymentTransactionState.RedirectedToBank;
        ledger.RawGatewayResponse = gatewayResult.RedirectUrl;
        await _ledgerRepository.UpdateAsync(ledger, cancellationToken);

        return new PaymentRequestResult
        {
            IsSuccess = true,
            TrackingNumber = trackingNumber,
            RedirectUrl = gatewayResult.RedirectUrl
        };
    }

    public async Task<PaymentVerifyResult> VerifyAsync(
        string trackingNumber,
        Money expectedAmountToman,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
            throw new ArgumentException("کد پیگیری الزامی است.", nameof(trackingNumber));

        var ledger = await _ledgerRepository.FirstOrDefaultAsync(l => l.TrackingNumber == trackingNumber, cancellationToken);
        if (ledger == null)
            return new PaymentVerifyResult { IsSuccess = false, ErrorMessage = "رکورد پرداخت متناظر با این کد پیگیری یافت نشد." };

        // ضد Replay: تراکنش قبلاً موفق شده — دوباره اعتبار نمی‌شود
        if (ledger.State == PaymentTransactionState.VerifiedSuccess)
            return new PaymentVerifyResult { IsSuccess = true, AlreadyProcessed = true, BankRefId = ledger.BankRefId };

        if (!_gateways.TryGetValue(ledger.GatewayName, out var gateway))
            return new PaymentVerifyResult { IsSuccess = false, ErrorMessage = "درگاه متناظر پشتیبانی نمی‌شود." };

        var apiKey = await GetGatewayApiKeyAsync(ledger.GatewayName, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
            return new PaymentVerifyResult { IsSuccess = false, ErrorMessage = "کلید API درگاه برای تایید یافت نشد." };

        var verifyResult = await gateway.VerifyAsync(new PaymentGatewayVerifyRequest
        {
            TrackingNumber = trackingNumber,
            AmountToman = expectedAmountToman.Toman,
            Extra = apiKey
        }, cancellationToken);

        // سه شرط سخت (سند ۹.۲): فراخوانی واقعی + موفق + تطابق مبلغ
        var verified = verifyResult.IsSuccess
            && ledger.AmountToman == expectedAmountToman.Toman;

        ledger.State = verified ? PaymentTransactionState.VerifiedSuccess : PaymentTransactionState.VerifiedFailed;
        ledger.BankRefId = verifyResult.BankRefId;
        ledger.RawGatewayResponse = verifyResult.ErrorMessage ?? verifyResult.BankRefId;
        ledger.VerifiedOnUtc = DateTime.UtcNow;
        await _ledgerRepository.UpdateAsync(ledger, cancellationToken);

        if (!verified)
            return new PaymentVerifyResult
            {
                IsSuccess = false,
                ErrorMessage = verifyResult.ErrorMessage ?? "تایید پرداخت ناموفق بود (پاسخ درگاه یا تطابق مبلغ)."
            };

        return new PaymentVerifyResult { IsSuccess = true, BankRefId = verifyResult.BankRefId };
    }
}
