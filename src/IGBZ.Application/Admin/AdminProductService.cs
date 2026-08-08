namespace IGBZ.Application.Admin;

using IGBZ.Application.Abstractions;
using IGBZ.Domain.Catalog;

public class AdminProductService : IAdminProductService
{
    private readonly ITenantScopedRepository<Product> _productRepository;

    public AdminProductService(ITenantScopedRepository<Product> productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IReadOnlyList<ProductListDto>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.FindAsync(p => !p.Deleted, cancellationToken);

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
                    .FirstOrDefault(),
                // برای ادمین: مشخص‌کردن وضعیت انتشار
                IsPublished = p.IsPublished
            })
            .ToList();
    }

    public async Task<string> CreateAsync(AdminProductInput input, CancellationToken cancellationToken = default)
    {
        Validate(input);

        // اسلاگ یکتا در سطح تننت
        var slug = input.Slug.Trim().ToLowerInvariant();
        var existingSlug = await _productRepository.FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);
        if (existingSlug != null)
            throw new InvalidOperationException("این اسلاگ قبلاً استفاده شده است.");

        // SKU یکتا در سطح تننت
        await EnsureUniqueSkusAsync(input, cancellationToken);

        var product = new Product
        {
            Name = input.Name.Trim(),
            Slug = slug,
            Description = input.Description,
            Images = input.Images,
            IsPublished = input.IsPublished,
            IsDigital = input.IsDigital,
            CreatedOnUtc = DateTime.UtcNow,
            Variants = input.Variants.Select(MapVariant).ToList()
        };

        await _productRepository.InsertAsync(product, cancellationToken);
        return product.Id;
    }

    public async Task UpdateAsync(string productId, AdminProductInput input, CancellationToken cancellationToken = default)
    {
        Validate(input);

        var product = await _productRepository.GetByIdAsync(productId, cancellationToken)
            ?? throw new InvalidOperationException("محصول یافت نشد.");

        var slug = input.Slug.Trim().ToLowerInvariant();
        var existingSlug = await _productRepository.FirstOrDefaultAsync(
            p => p.Slug == slug && p.Id != productId, cancellationToken);
        if (existingSlug != null)
            throw new InvalidOperationException("این اسلاگ قبلاً استفاده شده است.");

        await EnsureUniqueSkusAsync(input, productId, cancellationToken);

        product.Name = input.Name.Trim();
        product.Slug = slug;
        product.Description = input.Description;
        product.Images = input.Images;
        product.IsPublished = input.IsPublished;
        product.IsDigital = input.IsDigital;
        product.Variants = input.Variants.Select(MapVariant).ToList();
        product.UpdatedOnUtc = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product, cancellationToken);
    }

    public async Task DeleteAsync(string productId, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken)
            ?? throw new InvalidOperationException("محصول یافت نشد.");

        product.Deleted = true;
        product.UpdatedOnUtc = DateTime.UtcNow;
        await _productRepository.UpdateAsync(product, cancellationToken);
    }

    public async Task SetPublishedAsync(string productId, bool published, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken)
            ?? throw new InvalidOperationException("محصول یافت نشد.");

        product.IsPublished = published;
        product.UpdatedOnUtc = DateTime.UtcNow;
        await _productRepository.UpdateAsync(product, cancellationToken);
    }

    // ────────────────────────── ابزار ──────────────────────────

    private static void Validate(AdminProductInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new ArgumentException("نام محصول الزامی است.", nameof(input));
        if (string.IsNullOrWhiteSpace(input.Slug))
            throw new ArgumentException("اسلاگ محصول الزامی است.", nameof(input));
        if (input.Variants.Count == 0)
            throw new ArgumentException("حداقل یک واریانت لازم است.", nameof(input));
        if (input.Variants.Any(v => string.IsNullOrWhiteSpace(v.Sku) || v.PriceToman < 0 || v.StockQuantity < 0))
            throw new ArgumentException("واریانت نامعتبر است (SKU، قیمت و موجودی را بررسی کنید).", nameof(input));
    }

    private async Task EnsureUniqueSkusAsync(AdminProductInput input, string? exceptProductId = null, CancellationToken cancellationToken = default)
    {
        // یکتایی SKU داخل خودِ ورودی
        var dupInInput = input.Variants
            .GroupBy(v => v.Sku.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (dupInInput != null)
            throw new InvalidOperationException($"SKU تکراری در ورودی: {dupInInput.Key}");

        // یکتایی SKU در کل تننت
        var all = await _productRepository.FindAsync(_ => true, cancellationToken);
        var usedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in all)
        {
            if (exceptProductId != null && p.Id == exceptProductId)
                continue;
            foreach (var v in p.Variants)
                usedSkus.Add(v.Sku);
        }

        var conflict = input.Variants.FirstOrDefault(v => usedSkus.Contains(v.Sku.Trim()));
        if (conflict != null)
            throw new InvalidOperationException($"SKU «{conflict.Sku}» قبلاً در فروشگاه استفاده شده است.");
    }

    private static ProductVariant MapVariant(AdminProductVariantInput v) => new()
    {
        Sku = v.Sku.Trim(),
        Attributes = v.Attributes,
        PriceToman = v.PriceToman,
        OldPriceToman = v.OldPriceToman,
        StockQuantity = v.StockQuantity,
        ReservedQuantity = 0
    };
}
