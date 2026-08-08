namespace IGBZ.Application.Tests.Fakes;

using System.Linq.Expressions;
using IGBZ.Application.Abstractions;
using IGBZ.Domain.Common;

/// <summary>Repository در-حافظه برای موجودیت‌های سطح پلتفرم (بدون tenantId).</summary>
public class FakeRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
    private int _nextId = 1;

    public List<TEntity> Store { get; } = new();

    public Task<TEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Store.FirstOrDefault(e => e is Entity entity && entity.Id == id));
    }

    public Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<TEntity>>(Store.Where(predicate.Compile()).ToList());
    }

    public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Store.FirstOrDefault(predicate.Compile()));
    }

    public Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity is Entity baseEntity && string.IsNullOrWhiteSpace(baseEntity.Id))
            baseEntity.Id = (_nextId++).ToString();

        Store.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var index = Store.FindIndex(e => e is Entity baseEntity && baseEntity.Id == ((Entity)entity).Id);
        if (index < 0)
            throw new InvalidOperationException("موجودیت یافت نشد.");
        Store[index] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        Store.RemoveAll(e => e is Entity entity && entity.Id == id);
        return Task.CompletedTask;
    }
}
