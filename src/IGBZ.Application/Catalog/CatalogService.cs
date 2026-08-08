namespace IGBZ.Application.Catalog;

using IGBZ.Application.Abstractions;
using IGBZ.Domain.Catalog;

public class CatalogService : ICatalogService
{
    private readonly ITenantScopedRepository<Product> _productRepository;

    public CatalogService(ITenantScopedRepository<Product> productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IReadOnlyList<ProductListDto>> GetPublishedProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.FindAsync(
            p => p.IsPublished && !p.Deleted, cancellationToken);

        return products
            .OrderByDescending(p => p.CreatedOnUtc)
            .Select(p => new ProductListDto
            {
                Id = p.Id,
                Slug = p.Slug,
                Name = p.Name,
                ImageUrl = p.Images.FirstOrDefault(),
                PriceToman = p.GetLowestPriceToman(),
                OldPriceToman = p.Variants
                    .Where(v => v.OldPriceToman.HasValue)
                    .Select(v => v.OldPriceToman)
                    .OrderByDescending(v => v)
                    .FirstOrDefault()
            })
            .ToList();
    }

    public async Task<ProductDetailDto?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        var product = await _productRepository.FirstOrDefaultAsync(
            p => p.Slug == slug.Trim().ToLowerInvariant() && p.IsPublished && !p.Deleted,
            cancellationToken);

        if (product == null)
            return null;

        return new ProductDetailDto
        {
            Id = product.Id,
            Slug = product.Slug,
            Name = product.Name,
            Description = product.Description,
            Images = product.Images,
            IsDigital = product.IsDigital,
            Variants = product.Variants.Select(v => new ProductVariantDto
            {
                Sku = v.Sku,
                Attributes = v.Attributes,
                PriceToman = v.PriceToman,
                OldPriceToman = v.OldPriceToman,
                AvailableQuantity = v.AvailableQuantity
            }).ToList()
        };
    }
}
