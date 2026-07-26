using IGBZ.Application.Abstractions;
using IGBZ.Application.Orders;
using IGBZ.Domain.Orders;
using IGBZ.Domain.Payments;
using IGBZ.Domain.Shared;

namespace IGBZ.Application.Payments;

public sealed class PaymentApplicationService(
    ITenantContextAccessor tenantContextAccessor,
    ITenantScopedRepository<Order> orders,
    ITenantScopedRepository<PaymentIntent> paymentIntents,
    ITenantScopedRepository<PaymentTransaction> paymentTransactions,
    IPaymentGatewayService gatewayService,
    OrderService orderService,
    IClock clock)
{
    public async Task<CreatePaymentIntentResponse> CreateIntentAsync(CreatePaymentIntentRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.RequiredTenantId;
        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new DomainException("Order was not found.");

        if (order.Status != OrderStatus.Pending)
        {
            throw new DomainException("Only pending orders can be paid.");
        }

        var now = clock.UtcNow;
        var intent = PaymentIntent.Create(tenantId, order.Id, order.GrandTotal, order.Currency, gatewayService.GatewayName, now);

        var startResult = await gatewayService.StartAsync(
            new PaymentStartRequest(tenantId, intent.Id, order.Id, intent.Amount, intent.Currency, request.CallbackUrl),
            cancellationToken);

        intent.MarkRedirectCreated(startResult.GatewayReference, startResult.RedirectUrl, now);
        await paymentIntents.AddAsync(intent, cancellationToken);

        return new CreatePaymentIntentResponse(intent.Id, startResult.GatewayName, startResult.GatewayReference, startResult.RedirectUrl, intent.Status);
    }

    public async Task<PaymentCallbackResponse> HandleMockCallbackAsync(MockPaymentCallbackRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.RequiredTenantId;
        var intent = await paymentIntents.GetByIdAsync(request.PaymentIntentId, cancellationToken)
            ?? throw new DomainException("Payment intent was not found.");

        if (!string.Equals(intent.GatewayReference, request.GatewayReference, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("Gateway reference does not match this payment intent.");
        }

        if (intent.Amount != request.Amount || !string.Equals(intent.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("Payment amount or currency does not match the payment intent.");
        }

        if (intent.Status == PaymentStatus.Succeeded)
        {
            return new PaymentCallbackResponse(intent.Id, intent.OrderId, intent.Status, true, null);
        }

        var verification = await gatewayService.VerifyAsync(
            new PaymentVerificationRequest(tenantId, intent.Id, request.GatewayReference, request.Amount, request.Currency, request.IsSuccessful),
            cancellationToken);

        if (!verification.IsSuccessful)
        {
            intent.MarkFailed(verification.FailureReason ?? "Gateway verification failed.", clock.UtcNow);
            await paymentIntents.ReplaceAsync(intent, cancellationToken);
            return new PaymentCallbackResponse(intent.Id, intent.OrderId, intent.Status, false, intent.FailureReason);
        }

        var existingTransaction = await paymentTransactions.FirstOrDefaultAsync(
            transaction => transaction.GatewayReference == verification.GatewayReference,
            cancellationToken);

        var transaction = existingTransaction ?? PaymentTransaction.Create(
            tenantId,
            intent.Id,
            verification.GatewayName,
            verification.GatewayReference,
            intent.Amount,
            intent.Currency,
            PaymentStatus.Succeeded,
            clock.UtcNow);

        if (existingTransaction is null)
        {
            await paymentTransactions.AddAsync(transaction, cancellationToken);
        }

        await orderService.MarkOrderAsPaidAsync(intent.OrderId, transaction.Id, cancellationToken);
        intent.MarkSucceeded(clock.UtcNow);
        await paymentIntents.ReplaceAsync(intent, cancellationToken);

        return new PaymentCallbackResponse(intent.Id, intent.OrderId, intent.Status, true, null);
    }
}
