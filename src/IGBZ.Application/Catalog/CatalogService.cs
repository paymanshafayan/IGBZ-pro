using IGBZ.Application.Abstractions;
using IGBZ.Domain.Catalog;
using IGBZ.Domain.Shared;

namespace IGBZ.Application.Catalog;

public sealed class CatalogService(
    ITenantContextAccessor tenantContextAccessor,
    ITenantScopedRepository<Product> products,
    ITenantScopedRepository<Category> categories,
    IInventoryService inventoryService,
    IClock clock)
{
    public async Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.RequiredTenantId;
        var existing = await categories.FirstOrDefaultAsync(category => category.Slug == Product.NormalizeSlug(request.Slug), cancellationToken);
        if (existing is not null)
        {
            throw new DomainException("A category with this slug already exists.");
        }

        var category = Category.Create(tenantId, request.Name, request.Slug, request.ParentId, clock.UtcNow);
        await categories.AddAsync(category, cancellationToken);
        return ToCategoryResponse(category);
    }

    public async Task<IReadOnlyList<CategoryResponse>> ListCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var result = await categories.ListAsync(category => category.IsActive, cancellationToken);
        return result.OrderBy(category => category.Name).Select(ToCategoryResponse).ToList();
    }

    public async Task<ProductResponse> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Variants is null || request.Variants.Count == 0)
        {
            throw new DomainException("Product requires at least one variant.");
        }

        var tenantId = tenantContextAccessor.RequiredTenantId;
        var normalizedSlug = Product.NormalizeSlug(request.Slug);
        var existing = await products.FirstOrDefaultAsync(product => product.Slug == normalizedSlug, cancellationToken);
        if (existing is not null)
        {
            throw new DomainException("A product with this slug already exists.");
        }

        var now = clock.UtcNow;
        var product = Product.Create(tenantId, request.Name, normalizedSlug, request.Description, request.Type, now);

        if (request.CategoryIds is not null)
        {
            foreach (var categoryId in request.CategoryIds)
            {
                product.AddCategory(categoryId, now);
            }
        }

        foreach (var variantRequest in request.Variants)
        {
            var variant = product.AddVariant(
                variantRequest.Sku,
                variantRequest.Name,
                variantRequest.Price,
                variantRequest.Currency,
                variantRequest.Attributes,
                variantRequest.IsDefault,
                now);

            await inventoryService.EnsureInventoryItemAsync(product.Id, variant.Id, variantRequest.InitialStock, cancellationToken);
        }

        if (request.PublishImmediately)
        {
            product.Publish(now);
        }

        await products.AddAsync(product, cancellationToken);
        return ToProductResponse(product);
    }

    public async Task<IReadOnlyList<ProductResponse>> ListAdminProductsAsync(CancellationToken cancellationToken = default)
    {
        var result = await products.ListAsync(null, cancellationToken);
        return result.OrderByDescending(product => product.CreatedAtUtc).Select(ToProductResponse).ToList();
    }

    public async Task<IReadOnlyList<ProductResponse>> ListPublishedProductsAsync(CancellationToken cancellationToken = default)
    {
        var result = await products.ListAsync(product => product.Status == ProductStatus.Published, cancellationToken);
        return result.OrderByDescending(product => product.CreatedAtUtc).Select(ToProductResponse).ToList();
    }

    public async Task<ProductResponse> GetProductByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var product = await products.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Product was not found.");
        return ToProductResponse(product);
    }

    public async Task<ProductResponse> GetPublishedProductBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalized = Product.NormalizeSlug(slug);
        var product = await products.FirstOrDefaultAsync(candidate => candidate.Slug == normalized && candidate.Status == ProductStatus.Published, cancellationToken)
            ?? throw new DomainException("Product was not found.");
        return ToProductResponse(product);
    }

    public async Task<ProductResponse> PublishProductAsync(string id, CancellationToken cancellationToken = default)
    {
        var product = await products.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Product was not found.");

        product.Publish(clock.UtcNow);
        await products.ReplaceAsync(product, cancellationToken);
        return ToProductResponse(product);
    }

    private static CategoryResponse ToCategoryResponse(Category category)
        => new(category.Id, category.Name, category.Slug, category.ParentId, category.IsActive);

    public static ProductResponse ToProductResponse(Product product)
        => new(
            product.Id,
            product.TenantId,
            product.Name,
            product.Slug,
            product.Description,
            product.Type,
            product.Status,
            product.CategoryIds,
            product.Variants.Select(ToVariantResponse).ToList());

    private static ProductVariantResponse ToVariantResponse(ProductVariant variant)
        => new(
            variant.Id,
            variant.Sku,
            variant.Name,
            variant.Price,
            variant.Currency,
            variant.Attributes,
            variant.IsDefault,
            variant.IsActive);
}
