namespace IGBZ.Application.Abstractions;

public sealed record PaymentStartRequest(
    string TenantId,
    string PaymentIntentId,
    string OrderId,
    decimal Amount,
    string Currency,
    string CallbackUrl);

public sealed record PaymentStartResult(
    string GatewayName,
    string GatewayReference,
    string RedirectUrl);

public sealed record PaymentVerificationRequest(
    string TenantId,
    string PaymentIntentId,
    string GatewayReference,
    decimal Amount,
    string Currency,
    bool IsSuccessful);

public sealed record PaymentVerificationResult(
    string GatewayName,
    string GatewayReference,
    bool IsSuccessful,
    string? FailureReason);

public interface IPaymentGatewayService
{
    string GatewayName { get; }
    Task<PaymentStartResult> StartAsync(PaymentStartRequest request, CancellationToken cancellationToken = default);
    Task<PaymentVerificationResult> VerifyAsync(PaymentVerificationRequest request, CancellationToken cancellationToken = default);
}
