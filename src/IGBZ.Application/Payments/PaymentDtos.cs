using IGBZ.Domain.Payments;

namespace IGBZ.Application.Payments;

public sealed record CreatePaymentIntentRequest(string OrderId, string CallbackUrl);

public sealed record CreatePaymentIntentResponse(
    string PaymentIntentId,
    string GatewayName,
    string GatewayReference,
    string RedirectUrl,
    PaymentStatus Status);

public sealed record MockPaymentCallbackRequest(
    string PaymentIntentId,
    string GatewayReference,
    decimal Amount,
    string Currency,
    bool IsSuccessful);

public sealed record PaymentCallbackResponse(
    string PaymentIntentId,
    string OrderId,
    PaymentStatus Status,
    bool OrderMarkedAsPaid,
    string? FailureReason);
