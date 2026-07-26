namespace IGBZ.Application.Tenancy;

public sealed record TenantPlanDto(string Id, string Code, string Title, decimal MonthlyPrice, string Currency, bool IsActive);

public sealed record CreateTenantPlanRequest(string Code, string Title, decimal MonthlyPrice, string Currency);

public sealed record CheckSubdomainResponse(string Subdomain, bool IsAvailable, string? Reason);

public sealed record ProvisionTenantRequest(
    string StoreName,
    string Subdomain,
    string PlanCode,
    string AdminEmail,
    string AdminPhone);

public sealed record ProvisionTenantResponse(
    string TenantId,
    string StoreName,
    string Subdomain,
    string Host,
    string PlanCode,
    string Status);
