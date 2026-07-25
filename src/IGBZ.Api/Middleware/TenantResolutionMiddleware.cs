using IGBZ.Application.Abstractions;
using IGBZ.Domain.Tenancy;

namespace IGBZ.Api.Middleware;

public sealed class TenantResolutionMiddleware(
    RequestDelegate next,
    IPlatformDomainProvider domainProvider,
    ITenantContextAccessor tenantContextAccessor)
{
    public const string TenantHeaderName = "X-Tenant-Id";

    public async Task InvokeAsync(HttpContext context, IPlatformRepository<StoreDomainMapping> domainMappings)
    {
        try
        {
            if (IsInfrastructureRoute(context.Request.Path))
            {
                await next(context);
                return;
            }

            if (IsPlatformRoute(context.Request.Path))
            {
                tenantContextAccessor.Current = new TenantContext(TenantContext.PlatformTenantId, TenantResolutionSource.Platform);
                await next(context);
                return;
            }

            var resolved = await ResolveTenantAsync(context, domainMappings);
            if (resolved is null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "tenant_not_resolved",
                    message = $"Tenant could not be resolved. Send {TenantHeaderName}, a tenantId JWT claim, or use a mapped store domain."
                });
                return;
            }

            tenantContextAccessor.Current = resolved;
            await next(context);
        }
        finally
        {
            tenantContextAccessor.Current = null;
        }
    }

    private async Task<TenantContext?> ResolveTenantAsync(HttpContext context, IPlatformRepository<StoreDomainMapping> domainMappings)
    {
        var claimTenantId = context.User.FindFirst("tenantId")?.Value ?? context.User.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrWhiteSpace(claimTenantId))
        {
            return new TenantContext(claimTenantId.Trim(), TenantResolutionSource.Claim);
        }

        if (context.Request.Headers.TryGetValue(TenantHeaderName, out var headerTenantId) && !string.IsNullOrWhiteSpace(headerTenantId))
        {
            return new TenantContext(headerTenantId.ToString().Trim(), TenantResolutionSource.Header);
        }

        var host = context.Request.Host.Host;
        if (string.IsNullOrWhiteSpace(host) || string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var normalizedHost = StoreDomainMapping.NormalizeHost(host);
        var mapping = await domainMappings.FirstOrDefaultAsync(item => item.Host == normalizedHost);
        if (mapping is not null && mapping.VerificationStatus == DomainVerificationStatus.Verified)
        {
            return new TenantContext(mapping.TenantId, TenantResolutionSource.Host);
        }

        var rootDomain = StoreDomainMapping.NormalizeHost(domainProvider.RootDomain);
        if (normalizedHost.EndsWith($".{rootDomain}", StringComparison.OrdinalIgnoreCase))
        {
            var subdomain = normalizedHost[..^($".{rootDomain}".Length)];
            if (!string.IsNullOrWhiteSpace(subdomain) && !subdomain.Contains('.'))
            {
                var subdomainMapping = await domainMappings.FirstOrDefaultAsync(item => item.Host == normalizedHost);
                if (subdomainMapping is not null)
                {
                    return new TenantContext(subdomainMapping.TenantId, TenantResolutionSource.Host);
                }
            }
        }

        return null;
    }

    private static bool IsPlatformRoute(PathString path)
        => path.StartsWithSegments("/api/v1/platform", StringComparison.OrdinalIgnoreCase);

    private static bool IsInfrastructureRoute(PathString path)
        => path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);
}
