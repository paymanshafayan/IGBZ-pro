using System.Linq.Expressions;
using IGBZ.Domain.Shared;

namespace IGBZ.Application.Abstractions;

public interface IPlatformRepository<T>
    where T : Entity
{
    Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task ReplaceAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
