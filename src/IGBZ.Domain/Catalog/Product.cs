using IGBZ.Domain.Shared;

namespace IGBZ.Domain.Catalog;

public sealed class Product : TenantScopedEntity
{
    private Product()
    {
    }

    private Product(
        string tenantId,
        string name,
        string slug,
        string? description,
        ProductType type,
        DateTimeOffset now)
        : base(tenantId, now)
    {
        Name = Guard.AgainstEmpty(name, nameof(name));
        Slug = NormalizeSlug(slug);
        Description = description;
        Type = type;
        Status = ProductStatus.Draft;
    }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ProductType Type { get; private set; }
    public ProductStatus Status { get; private set; }
    public List<string> CategoryIds { get; private set; } = [];
    public List<ProductVariant> Variants { get; private set; } = [];
    public List<ProductMedia> Media { get; private set; } = [];

    public static Product Create(
        string tenantId,
        string name,
        string slug,
        string? description,
        ProductType type,
        DateTimeOffset now)
        => new(tenantId, name, slug, description, type, now);

    public void AddCategory(string categoryId, DateTimeOffset now)
    {
        categoryId = Guard.AgainstEmpty(categoryId, nameof(categoryId));
        if (!CategoryIds.Contains(categoryId, StringComparer.OrdinalIgnoreCase))
        {
            CategoryIds.Add(categoryId);
            Touch(now);
        }
    }

    public ProductVariant AddVariant(
        string sku,
        string name,
        decimal price,
        string currency,
        IReadOnlyDictionary<string, string>? attributes,
        bool isDefault,
        DateTimeOffset now)
    {
        if (Variants.Any(v => string.Equals(v.Sku, sku, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException($"Variant SKU '{sku}' already exists on this product.");
        }

        var variant = new ProductVariant(sku, name, price, currency, attributes, isDefault || Variants.Count == 0);
        if (variant.IsDefault)
        {
            foreach (var existing in Variants)
            {
                existing.ClearDefault();
            }
        }

        Variants.Add(variant);
        Touch(now);
        return variant;
    }

    public ProductVariant GetActiveVariant(string variantId)
    {
        var variant = Variants.FirstOrDefault(v => v.Id == variantId && v.IsActive);
        return variant ?? throw new DomainException("Product variant was not found or is inactive.");
    }

    public void Publish(DateTimeOffset now)
    {
        if (Variants.Count == 0)
        {
            throw new DomainException("Product must have at least one variant before publishing.");
        }

        Status = ProductStatus.Published;
        Touch(now);
    }

    public void Unpublish(DateTimeOffset now)
    {
        Status = ProductStatus.Draft;
        Touch(now);
    }

    public static string NormalizeSlug(string slug)
    {
        var value = Guard.AgainstEmpty(slug, nameof(slug)).ToLowerInvariant();
        if (value.Any(ch => !char.IsAsciiLetterOrDigit(ch) && ch != '-'))
        {
            throw new DomainException("Slug can contain only ASCII letters, digits and dash.");
        }

        return value;
    }
}
