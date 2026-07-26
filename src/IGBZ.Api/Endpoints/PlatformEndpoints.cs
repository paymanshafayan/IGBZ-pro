using IGBZ.Application.Tenancy;

namespace IGBZ.Api.Endpoints;

public static class PlatformEndpoints
{
    public static IEndpointRouteBuilder MapPlatformEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/platform").WithTags("Platform");

        group.MapGet("/plans", async (TenantProvisioningService service, CancellationToken cancellationToken)
            => Results.Ok(await service.ListPlansAsync(cancellationToken)));

        group.MapPost("/plans", async (CreateTenantPlanRequest request, TenantProvisioningService service, CancellationToken cancellationToken)
            => Results.Created("/api/v1/platform/plans", await service.CreatePlanAsync(request, cancellationToken)));

        group.MapGet("/onboarding/subdomains/{subdomain}", async (string subdomain, TenantProvisioningService service, CancellationToken cancellationToken)
            => Results.Ok(await service.CheckSubdomainAsync(subdomain, cancellationToken)));

        group.MapPost("/onboarding/provision", async (ProvisionTenantRequest request, TenantProvisioningService service, CancellationToken cancellationToken)
            => Results.Created("/api/v1/platform/onboarding/provision", await service.ProvisionAsync(request, cancellationToken)));

        return endpoints;
    }
}
