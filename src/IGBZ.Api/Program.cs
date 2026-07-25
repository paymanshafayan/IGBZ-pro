using System.Text;
using System.Text.Json.Serialization;
using IGBZ.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using IGBZ.Api.Endpoints;
using IGBZ.Api.Middleware;
using IGBZ.Application;
using IGBZ.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIgbzApplication();
builder.Services.AddIgbzInfrastructure(builder.Configuration);

var jwtDefaults = new JwtOptions();
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = new JwtOptions
{
    Issuer = jwtSection["Issuer"] ?? jwtDefaults.Issuer,
    Audience = jwtSection["Audience"] ?? jwtDefaults.Audience,
    SigningKey = jwtSection["SigningKey"] ?? jwtDefaults.SigningKey,
    AccessTokenMinutes = int.TryParse(jwtSection["AccessTokenMinutes"], out var accessTokenMinutes)
        ? accessTokenMinutes
        : jwtDefaults.AccessTokenMinutes
};
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("TenantAdmin", policy => policy.RequireRole("TenantOwner", "TenantAdmin"));
});
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowAnyOrigin());
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new
{
    service = "IGBZ.Api",
    status = "ok",
    utc = DateTimeOffset.UtcNow
}));

app.MapPlatformEndpoints();
app.MapAuthEndpoints();
app.MapAdminEndpoints();
app.MapStorefrontEndpoints();
app.MapPaymentEndpoints();

app.Run();
