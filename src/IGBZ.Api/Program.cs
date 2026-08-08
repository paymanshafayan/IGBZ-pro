using System.Text;
using IGBZ.Api.Middleware;
using IGBZ.Application;
using IGBZ.Infrastructure;
using IGBZ.Infrastructure.Auth;
using IGBZ.Infrastructure.Mongo;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

// ── زیرساخت (MongoDB + JWT + درگاه تست) ──
var mongoOptions = new MongoOptions
{
    ConnectionString = builder.Configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017",
    DatabaseName = builder.Configuration["Mongo:Database"] ?? "igbz"
};

var jwtOptions = new JwtOptions
{
    SigningKey = builder.Configuration["Jwt:SigningKey"] ?? "dev-only-signing-key-change-me-0123456789abcdef",
    Issuer = builder.Configuration["Jwt:Issuer"] ?? "igbz",
    Audience = builder.Configuration["Jwt:Audience"] ?? "igbz-apps",
    ExpiryMinutes = int.TryParse(builder.Configuration["Jwt:ExpiryMinutes"], out var exp) ? exp : 60 * 24 * 7
};

builder.Services.AddIgBzInfrastructure(mongoOptions, jwtOptions);

// ── لایهٔ اپلیکیشن (موتور تجارت) ──
builder.Services.AddIgBzApplication();

// انتشار خودکار پست اینستاگرام (Consumer)
builder.Services.AddScoped<IGBZ.Api.Consumers.IProductInstagramPublisher, IGBZ.Api.Consumers.ProductInstagramPublisher>();

// ── احراز هویت JWT ──
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    // فقط مالک/ادمین تننت (توکن با ادعای isTenantOwner=True)
    options.AddPolicy("TenantOwner", policy => policy.RequireClaim("isTenantOwner", "True"));
});
builder.Services.AddControllers();

var app = builder.Build();

// Rate Limiting (سند بخش ۱۶): per-IP + per-tenant
var rateLimit = int.TryParse(builder.Configuration["RateLimiting:RequestsPerMinute"], out var rl) ? rl : 120;
app.UseMiddleware<RateLimitingMiddleware>(app.Services.GetRequiredService<IMemoryCache>(), rateLimit);

app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "igbz-api", utc = DateTime.UtcNow }));

app.Run();

// برای تست‌های Integration در آینده
public partial class Program;
