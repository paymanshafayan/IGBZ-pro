using IGBZ.Application.Abstractions;
using IGBZ.Domain.Shared;

namespace IGBZ.Infrastructure.Tenancy;

public sealed class TenantContextAccessor : ITenantContextAccessor
{
    private static readonly AsyncLocal<TenantContextHolder?> CurrentHolder = new();

    public TenantContext? Current
    {
        get => CurrentHolder.Value?.Context;
        set
        {
            var holder = CurrentHolder.Value;
            if (holder is not null)
            {
                holder.Context = null;
            }

            if (value is not null)
            {
                CurrentHolder.Value = new TenantContextHolder { Context = value };
            }
        }
    }

    public string RequiredTenantId => Current?.TenantId
        ?? throw new DomainException("Tenant context was not resolved for this request.");

    private sealed class TenantContextHolder
    {
        public TenantContext? Context { get; set; }
    }
}
