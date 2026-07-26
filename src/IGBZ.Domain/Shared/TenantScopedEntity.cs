namespace IGBZ.Domain.Shared;

public abstract class TenantScopedEntity : Entity, ITenantScopedEntity
{
    protected TenantScopedEntity()
    {
    }

    protected TenantScopedEntity(string tenantId, DateTimeOffset now)
        : base(now)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new DomainException("TenantId is required for tenant-scoped documents.");
        }

        TenantId = tenantId.Trim();
    }

    public string TenantId { get; protected set; } = string.Empty;
}
