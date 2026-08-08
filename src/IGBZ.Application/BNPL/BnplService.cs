namespace IGBZ.Application.BNPL;

using IGBZ.Application.Abstractions;
using IGBZ.Domain.Integration;

public class BnplStartResult
{
    public bool IsSuccess { get; init; }
    public string? RedirectUrl { get; init; }
    public string? TransactionId { get; init; }
    public string? PaymentToken { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>مدیریت پرداخت اعتباری (BNPL) — دیجی‌پی/اسنپ‌پی با اعتبارنامهٔ تننت.</summary>
public interface IBnplService
{
    Task<BnplEligibilityResult> CheckEligibilityAsync(
        string providerKey, decimal amountToman, string customerMobile, string? customerNationalId,
        CancellationToken cancellationToken = default);

    Task<BnplStartResult> StartPaymentAsync(
        string providerKey, string orderId, decimal amountToman, string callbackUrl, string customerMobile,
        CancellationToken cancellationToken = default);

    Task<BnplVerifyResult> VerifyPaymentAsync(
        string providerKey, string transactionId, string paymentToken,
        CancellationToken cancellationToken = default);
}

public class BnplService : IBnplService
{
    private readonly ITenantScopedRepository<IntegrationCredential> _credentialRepository;
    private readonly IReadOnlyDictionary<string, IBnplGateway> _gateways;

    public BnplService(
        ITenantScopedRepository<IntegrationCredential> credentialRepository,
        IEnumerable<IBnplGateway> gateways)
    {
        _credentialRepository = credentialRepository;
        _gateways = gateways.ToDictionary(g => g.ProviderKey, g => g, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<string?> GetApiKeyAsync(string providerKey, CancellationToken cancellationToken)
    {
        var credential = await _credentialRepository.FirstOrDefaultAsync(
            c => c.ProviderKey == providerKey && c.IsActive, cancellationToken);
        return credential?.ApiKeyEncrypted;
    }

    public async Task<BnplEligibilityResult> CheckEligibilityAsync(
        string providerKey, decimal amountToman, string customerMobile, string? customerNationalId,
        CancellationToken cancellationToken = default)
    {
        if (!_gateways.TryGetValue(providerKey, out var gateway))
            return new BnplEligibilityResult { IsEligible = false, Message = "ارائه‌دهندهٔ BNPL پشتیبانی نمی‌شود." };

        var apiKey = await GetApiKeyAsync(providerKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
            return new BnplEligibilityResult { IsEligible = false, Message = "اعتبارنامهٔ این ارائه‌دهنده فعال نیست." };

        return await gateway.CheckEligibilityAsync(new BnplEligibilityRequest
        {
            AmountToman = amountToman,
            CustomerMobile = customerMobile,
            CustomerNationalId = customerNationalId
        }, apiKey, cancellationToken);
    }

    public async Task<BnplStartResult> StartPaymentAsync(
        string providerKey, string orderId, decimal amountToman, string callbackUrl, string customerMobile,
        CancellationToken cancellationToken = default)
    {
        if (!_gateways.TryGetValue(providerKey, out var gateway))
            return new BnplStartResult { IsSuccess = false, ErrorMessage = "ارائه‌دهندهٔ BNPL پشتیبانی نمی‌شود." };

        var apiKey = await GetApiKeyAsync(providerKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
            return new BnplStartResult { IsSuccess = false, ErrorMessage = "اعتبارنامهٔ این ارائه‌دهنده فعال نیست." };

        var transactionId = $"BNPL-{orderId}-{Guid.NewGuid():N}"[..Math.Min(40, $"BNPL-{orderId}-{Guid.NewGuid():N}".Length)];

        var result = await gateway.RequestPaymentAsync(new BnplPaymentRequest
        {
            TransactionId = transactionId,
            AmountToman = amountToman,
            CallbackUrl = callbackUrl,
            CustomerMobile = customerMobile
        }, apiKey, cancellationToken);

        return new BnplStartResult
        {
            IsSuccess = result.IsSuccess,
            RedirectUrl = result.RedirectUrl,
            PaymentToken = result.PaymentToken,
            TransactionId = transactionId,
            ErrorMessage = result.ErrorMessage
        };
    }

    public async Task<BnplVerifyResult> VerifyPaymentAsync(
        string providerKey, string transactionId, string paymentToken,
        CancellationToken cancellationToken = default)
    {
        if (!_gateways.TryGetValue(providerKey, out var gateway))
            return new BnplVerifyResult { IsSuccess = false, ErrorMessage = "ارائه‌دهندهٔ BNPL پشتیبانی نمی‌شود." };

        var apiKey = await GetApiKeyAsync(providerKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
            return new BnplVerifyResult { IsSuccess = false, ErrorMessage = "اعتبارنامهٔ این ارائه‌دهنده فعال نیست." };

        return await gateway.VerifyPaymentAsync(new BnplVerifyRequest
        {
            TransactionId = transactionId,
            PaymentToken = paymentToken
        }, apiKey, cancellationToken);
    }
}
