using IGBZ.Domain.Shared;

namespace IGBZ.Domain.Catalog;

public sealed class Category : TenantScopedEntity
{
    private Category()
    {
    }

    private Category(string tenantId, string name, string slug, string? parentId, DateTimeOffset now)
        : base(tenantId, now)
    {
        Name = Guard.AgainstEmpty(name, nameof(name));
        Slug = Product.NormalizeSlug(slug);
        ParentId = parentId;
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? ParentId { get; private set; }
    public bool IsActive { get; private set; }

    public static Category Create(string tenantId, string name, string slug, string? parentId, DateTimeOffset now)
        => new(tenantId, name, slug, parentId, now);
}
