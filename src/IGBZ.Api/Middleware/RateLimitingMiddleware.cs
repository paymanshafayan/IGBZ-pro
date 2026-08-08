namespace IGBZ.Api.Middleware;

using Microsoft.Extensions.Caching.Memory;

/// <summary>
/// Rate Limiting ساده با پنجرهٔ ثابت (سند بخش ۱۶): per-IP + per-tenant.
/// اگر از حد مجاز (پیش‌فرض ۱۲۰ در دقیقه) بیشتر شود → 429.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly int _requestsPerMinute;

    public RateLimitingMiddleware(RequestDelegate next, IMemoryCache cache, int requestsPerMinute = 120)
    {
        _next = next;
        _cache = cache;
        _requestsPerMinute = requestsPerMinute;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var tenant = context.Request.Headers["X-Tenant-Id"].ToString() ?? "platform";

        var key = $"rl:{ip}:{tenant}";
        var count = _cache.TryGetValue(key, out int current) ? current : 0;

        if (count >= _requestsPerMinute)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { success = false, message = "تعداد درخواست‌ها بیش از حد مجاز است. کمی بعد دوباره تلاش کنید." });
            return;
        }

        _cache.Set(key, count + 1, TimeSpan.FromMinutes(1));
        await _next(context);
    }
}
