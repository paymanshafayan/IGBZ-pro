using IGBZ.Domain.Catalog;

namespace IGBZ.Application.Catalog;

public sealed record CreateCategoryRequest(string Name, string Slug, string? ParentId);

public sealed record CategoryResponse(string Id, string Name, string Slug, string? ParentId, bool IsActive);

public sealed record CreateVariantRequest(
    string Sku,
    string Name,
    decimal Price,
    string Currency,
    Dictionary<string, string>? Attributes,
    int InitialStock,
    bool IsDefault);

public sealed record CreateProductRequest(
    string Name,
    string Slug,
    string? Description,
    ProductType Type,
    IReadOnlyList<string>? CategoryIds,
    IReadOnlyList<CreateVariantRequest> Variants,
    bool PublishImmediately);

public sealed record ProductVariantResponse(
    string Id,
    string Sku,
    string Name,
    decimal Price,
    string Currency,
    Dictionary<string, string> Attributes,
    bool IsDefault,
    bool IsActive);

public sealed record ProductResponse(
    string Id,
    string TenantId,
    string Name,
    string Slug,
    string? Description,
    ProductType Type,
    ProductStatus Status,
    IReadOnlyList<string> CategoryIds,
    IReadOnlyList<ProductVariantResponse> Variants);
