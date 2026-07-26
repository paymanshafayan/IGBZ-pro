using IGBZ.Application.Payments;

namespace IGBZ.Api.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/payments").WithTags("Payments");

        group.MapPost("/intents", async (CreatePaymentIntentRequest request, PaymentApplicationService service, CancellationToken cancellationToken)
            => Results.Created("/api/v1/payments/intents", await service.CreateIntentAsync(request, cancellationToken)));

        group.MapPost("/mock-callback", async (MockPaymentCallbackRequest request, PaymentApplicationService service, CancellationToken cancellationToken)
            => Results.Ok(await service.HandleMockCallbackAsync(request, cancellationToken)));

        return endpoints;
    }
}
