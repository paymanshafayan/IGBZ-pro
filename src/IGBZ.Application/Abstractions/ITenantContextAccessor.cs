namespace IGBZ.Application.Abstractions;

public interface ITenantContextAccessor
{
    TenantContext? Current { get; set; }
    string RequiredTenantId { get; }
}
