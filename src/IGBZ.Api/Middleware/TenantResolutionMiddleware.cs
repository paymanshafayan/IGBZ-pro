namespace IGBZ.Api.Middleware;

using IGBZ.Application.Auth;
using IGBZ.Application.Tenancy;

/// <summary>
/// تشخیص تننت جاری (سند بخش ۵): هدر X-Tenant-Id ← JWT claim ← Host.
/// اگر هیچ‌کدام نبود، بافت خالی می‌ماند و سرویس‌های تننت‌محور خطای صریح می‌دهند.
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IJwtTokenService _jwtTokenService;

    public TenantResolutionMiddleware(RequestDelegate next, IJwtTokenService jwtTokenService)
    {
        _next = next;
        _jwtTokenService = jwtTokenService;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        // ۱) هدر صریح X-Tenant-Id (اولویت اول — برای اپ موبایل/API)
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerValue) &&
            !string.IsNullOrWhiteSpace(headerValue.ToString()))
        {
            tenantContext.Set(headerValue.ToString().Trim());
            await _next(context);
            return;
        }

        // ۲) JWT Bearer → ادعای tenantId
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();
            var payload = _jwtTokenService.ValidateAndDecode(token);
            if (payload != null)
            {
                tenantContext.Set(payload.TenantId);
                await _next(context);
                return;
            }
        }

        // ۳) Host → زیردامنه یا platform
        var host = context.Request.Host.Host?.Trim();
        if (!string.IsNullOrWhiteSpace(host))
        {
            var firstLabel = host.Split('.')[0].ToLowerInvariant();
            var resolved = firstLabel is "localhost" or "api" or "www"
                ? "platform"
                : firstLabel;

            tenantContext.Set(resolved);
            await _next(context);
            return;
        }

        await _next(context);
    }
}
