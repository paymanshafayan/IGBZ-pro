namespace IGBZ.Application.Tests.Fakes;

using IGBZ.Application.Abstractions;
using IGBZ.Application.Tenancy;
using IGBZ.Domain.Catalog;

/// <summary>پیاده‌سازی در-حافظهٔ موجودی با رزرو — برای تست OrderService.</summary>
public class FakeProductInventoryRepository : IProductInventoryRepository
{
    private readonly ITenantContext _tenantContext;
    private int _nextId = 1;

    public FakeProductInventoryRepository(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public List<Product> Store { get; } = new();

    private string RequireTenant() =>
        _tenantContext.TenantId ?? throw new TenantRequiredException(nameof(Product));

    public Task<Product?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var tenant = RequireTenant();
        return Task.FromResult(Store.FirstOrDefault(p => p.Id == id && p.TenantId == tenant));
    }

    public Task<IReadOnlyList<Product>> FindAsync(
        System.Linq.Expressions.Expression<Func<Product, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var tenant = RequireTenant();
        var compiled = predicate.Compile();
        return Task.FromResult<IReadOnlyList<Product>>(Store.Where(p => p.TenantId == tenant && compiled(p)).ToList());
    }

    public Task<Product?> FirstOrDefaultAsync(
        System.Linq.Expressions.Expression<Func<Product, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var tenant = RequireTenant();
        var compiled = predicate.Compile();
        return Task.FromResult(Store.FirstOrDefault(p => p.TenantId == tenant && compiled(p)));
    }

    public Task<int> CountAsync(System.Linq.Expressions.Expression<Func<Product, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var tenant = RequireTenant();
        var compiled = predicate.Compile();
        return Task.FromResult(Store.Count(p => p.TenantId == tenant && compiled(p)));
    }

    public Task InsertAsync(Product entity, CancellationToken cancellationToken = default)
    {
        var tenant = RequireTenant();
        if (string.IsNullOrWhiteSpace(entity.TenantId))
            entity.TenantId = tenant;
        if (string.IsNullOrWhiteSpace(entity.Id))
            entity.Id = (_nextId++).ToString();
        Store.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Product entity, CancellationToken cancellationToken = default)
    {
        var tenant = RequireTenant();
        var index = Store.FindIndex(p => p.Id == entity.Id && p.TenantId == tenant);
        if (index < 0)
            throw new InvalidOperationException("محصول یافت نشد.");
        Store[index] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var tenant = RequireTenant();
        Store.RemoveAll(p => p.Id == id && p.TenantId == tenant);
        return Task.CompletedTask;
    }

    public Task<bool> TryReserveAsync(string productId, string sku, int quantity, CancellationToken cancellationToken = default)
    {
        var tenant = RequireTenant();
        var product = Store.FirstOrDefault(p => p.Id == productId && p.TenantId == tenant);
        if (product == null)
            return Task.FromResult(false);

        var variant = product.GetVariant(sku);
        if (variant == null || variant.AvailableQuantity < quantity)
            return Task.FromResult(false);

        variant.ReservedQuantity += quantity;
        return Task.FromResult(true);
    }

    public Task ReleaseReservationAsync(string productId, string sku, int quantity, CancellationToken cancellationToken = default)
    {
        var tenant = RequireTenant();
        var product = Store.FirstOrDefault(p => p.Id == productId && p.TenantId == tenant);
        var variant = product?.GetVariant(sku);
        if (variant != null)
            variant.ReservedQuantity = Math.Max(0, variant.ReservedQuantity - quantity);
        return Task.CompletedTask;
    }
}
