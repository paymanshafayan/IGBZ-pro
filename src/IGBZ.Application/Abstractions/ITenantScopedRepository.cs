namespace IGBZ.Application.Abstractions;

using System.Linq.Expressions;
using IGBZ.Domain.Common;

/// <summary>
/// Repository تننت‌محور — Generic Constraint روی <see cref="ITenantEntity"/> یعنی فقط موجودیت‌های
/// تننت‌دار از این مسیر قابل دسترسی‌اند (قفل کامپایل‌تایم جداسازی). بدون TenantContext،
/// هر متد <see cref="Tenancy.TenantRequiredException"/> پرتاب می‌کند.
/// </summary>
public interface ITenantScopedRepository<TEntity> where TEntity : class, ITenantEntity
{
    Task<TEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>درج — مقدار TenantId از بافت جاری ست می‌شود (و اگر مغایرت داشته باشد خطا می‌دهد).</summary>
    Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
