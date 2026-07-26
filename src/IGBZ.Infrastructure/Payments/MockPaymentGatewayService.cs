using IGBZ.Application.Abstractions;

namespace IGBZ.Infrastructure.Payments;

public sealed class MockPaymentGatewayService : IPaymentGatewayService
{
    public string GatewayName => "mock";

    public Task<PaymentStartResult> StartAsync(PaymentStartRequest request, CancellationToken cancellationToken = default)
    {
        var reference = $"MOCK-{Guid.NewGuid():N}";
        var redirectUrl = $"https://mock-payments.igbz.local/pay/{request.PaymentIntentId}?ref={reference}";
        return Task.FromResult(new PaymentStartResult(GatewayName, reference, redirectUrl));
    }

    public Task<PaymentVerificationResult> VerifyAsync(PaymentVerificationRequest request, CancellationToken cancellationToken = default)
    {
        var result = request.IsSuccessful
            ? new PaymentVerificationResult(GatewayName, request.GatewayReference, true, null)
            : new PaymentVerificationResult(GatewayName, request.GatewayReference, false, "Mock gateway returned unsuccessful payment.");

        return Task.FromResult(result);
    }
}
