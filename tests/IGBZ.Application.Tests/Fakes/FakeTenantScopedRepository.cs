namespace IGBZ.Application.Tests.Fakes;

using System.Linq.Expressions;
using IGBZ.Application.Abstractions;
using IGBZ.Application.Tenancy;
using IGBZ.Domain.Common;

/// <summary>
/// Repository در-حافظه برای تست‌ها — دقیقاً همان قانون جداسازی را اجرا می‌کند:
/// بدون بافت تننت، TenantRequiredException (سند بخش ۵).
/// </summary>
public class FakeTenantScopedRepository<TEntity> : ITenantScopedRepository<TEntity>
    where TEntity : class, ITenantEntity
{
    private readonly ITenantContext _tenantContext;
    private int _nextId = 1;

    public FakeTenantScopedRepository(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public List<TEntity> Store { get; } = new();

    private string RequireTenant() =>
        _tenantContext.TenantId ?? throw new TenantRequiredException(typeof(TEntity).Name);

    public Task<TEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var tenant = RequireTenant();
        return Task.FromResult(Store.FirstOrDefault(e => e.Id == id && e.TenantId == tenant));
    }

    public Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var tenant = RequireTenant();
        var compiled = predicate.Compile();
        return Task.FromResult<IReadOnlyList<TEntity>>(Store.Where(e => e.TenantId == tenant && compiled(e)).ToList());
    }

    public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var tenant = RequireTenant();
        var compiled = predicate.Compile();
        return Task.FromResult(Store.FirstOrDefault(e => e.TenantId == tenant && compiled(e)));
    }

    public Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var tenant = RequireTenant();
        var compiled = predicate.Compile();
        return Task.FromResult(Store.Count(e => e.TenantId == tenant && compiled(e)));
    }

    public Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var tenant = RequireTenant();

        if (string.IsNullOrWhiteSpace(entity.TenantId))
            entity.TenantId = tenant;
        else if (entity.TenantId != tenant)
            throw new InvalidOperationException("TenantId ناسازگار با بافت تننت جاری.");

        if (string.IsNullOrWhiteSpace(entity.Id))
            entity.Id = (_nextId++).ToString();

        Store.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var tenant = RequireTenant();
        var index = Store.FindIndex(e => e.Id == entity.Id && e.TenantId == tenant);
        if (index < 0)
            throw new InvalidOperationException("موجودیت یافت نشد.");
        Store[index] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var tenant = RequireTenant();
        Store.RemoveAll(e => e.Id == id && e.TenantId == tenant);
        return Task.CompletedTask;
    }
}
