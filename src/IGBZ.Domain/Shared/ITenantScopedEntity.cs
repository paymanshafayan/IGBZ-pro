namespace IGBZ.Domain.Shared;

public interface ITenantScopedEntity
{
    string Id { get; }
    string TenantId { get; }
}
