using IGBZ.Application.Auth;

namespace IGBZ.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/otp/request", async (RequestOtpRequest request, AuthService service, CancellationToken cancellationToken)
            => Results.Ok(await service.RequestOtpAsync(request, cancellationToken)));

        group.MapPost("/otp/verify", async (VerifyOtpRequest request, AuthService service, CancellationToken cancellationToken)
            => Results.Ok(await service.VerifyOtpAsync(request, cancellationToken)));

        return endpoints;
    }
}
