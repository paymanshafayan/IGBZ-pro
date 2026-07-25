using IGBZ.Domain.Shared;

namespace IGBZ.Domain.Payments;

public sealed class PaymentTransaction : TenantScopedEntity
{
    private PaymentTransaction()
    {
    }

    private PaymentTransaction(string tenantId, string paymentIntentId, string gatewayName, string gatewayReference, decimal amount, string currency, PaymentStatus status, DateTimeOffset now)
        : base(tenantId, now)
    {
        PaymentIntentId = Guard.AgainstEmpty(paymentIntentId, nameof(paymentIntentId));
        GatewayName = Guard.AgainstEmpty(gatewayName, nameof(gatewayName));
        GatewayReference = Guard.AgainstEmpty(gatewayReference, nameof(gatewayReference));
        Amount = Guard.AgainstNegative(amount, nameof(amount));
        Currency = Guard.AgainstEmpty(currency, nameof(currency)).ToUpperInvariant();
        Status = status;
    }

    public string PaymentIntentId { get; private set; } = string.Empty;
    public string GatewayName { get; private set; } = string.Empty;
    public string GatewayReference { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "IRR";
    public PaymentStatus Status { get; private set; }

    public static PaymentTransaction Create(string tenantId, string paymentIntentId, string gatewayName, string gatewayReference, decimal amount, string currency, PaymentStatus status, DateTimeOffset now)
        => new(tenantId, paymentIntentId, gatewayName, gatewayReference, amount, currency, status, now);
}
