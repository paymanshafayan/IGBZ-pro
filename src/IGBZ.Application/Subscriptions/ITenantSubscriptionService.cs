namespace IGBZ.Application.Subscriptions;

using IGBZ.Application.Payments;
using IGBZ.Domain.Common;

public class SubscriptionStatusResult
{
    public bool HasSubscription { get; init; }
    public bool IsUsable { get; init; }
    public string? PlanName { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? NextBillingDateUtc { get; init; }
}

/// <summary>مدیریت اشتراک تننت — پرداخت و فعال‌سازی پس از تایید واقعی درگاه.</summary>
public interface ITenantSubscriptionService
{
    Task<SubscriptionStatusResult> GetStatusAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>درخواست پرداخت برای اشتراک جاری (پلن پولی) — بازگشت لینک درگاه.</summary>
    Task<PaymentRequestResult> RequestPaymentAsync(string tenantId, string gatewayName, string callbackUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// تایید پرداخت (سه شرط سخت بخش ۹.۲) و فعال‌سازی اشتراک + تننت.
    /// در صورت AlreadyProcessed نیز فعال‌سازی Idempotent انجام می‌شود.
    /// </summary>
    Task<PaymentVerifyResult> VerifyAndActivateAsync(string tenantId, string trackingNumber, CancellationToken cancellationToken = default);

    Task<bool> ActivateAsync(string tenantId, CancellationToken cancellationToken = default);
}
