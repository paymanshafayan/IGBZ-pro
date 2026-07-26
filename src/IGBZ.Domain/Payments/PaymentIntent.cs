using IGBZ.Domain.Shared;

namespace IGBZ.Domain.Payments;

public sealed class PaymentIntent : TenantScopedEntity
{
    private PaymentIntent()
    {
    }

    private PaymentIntent(string tenantId, string orderId, decimal amount, string currency, string gatewayName, DateTimeOffset now)
        : base(tenantId, now)
    {
        OrderId = Guard.AgainstEmpty(orderId, nameof(orderId));
        Amount = Guard.AgainstNegative(amount, nameof(amount));
        Currency = Guard.AgainstEmpty(currency, nameof(currency)).ToUpperInvariant();
        GatewayName = Guard.AgainstEmpty(gatewayName, nameof(gatewayName));
        Status = PaymentStatus.Pending;
    }

    public string OrderId { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "IRR";
    public string GatewayName { get; private set; } = string.Empty;
    public PaymentStatus Status { get; private set; }
    public string? GatewayReference { get; private set; }
    public string? RedirectUrl { get; private set; }
    public string? FailureReason { get; private set; }

    public static PaymentIntent Create(string tenantId, string orderId, decimal amount, string currency, string gatewayName, DateTimeOffset now)
        => new(tenantId, orderId, amount, currency, gatewayName, now);

    public void MarkRedirectCreated(string gatewayReference, string redirectUrl, DateTimeOffset now)
    {
        GatewayReference = Guard.AgainstEmpty(gatewayReference, nameof(gatewayReference));
        RedirectUrl = Guard.AgainstEmpty(redirectUrl, nameof(redirectUrl));
        Status = PaymentStatus.RedirectCreated;
        Touch(now);
    }

    public void MarkSucceeded(DateTimeOffset now)
    {
        Status = PaymentStatus.Succeeded;
        Touch(now);
    }

    public void MarkFailed(string reason, DateTimeOffset now)
    {
        FailureReason = Guard.AgainstEmpty(reason, nameof(reason));
        Status = PaymentStatus.Failed;
        Touch(now);
    }
}
