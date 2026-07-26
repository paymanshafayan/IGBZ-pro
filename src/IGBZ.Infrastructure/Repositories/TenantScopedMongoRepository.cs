using System.Linq.Expressions;
using IGBZ.Application.Abstractions;
using IGBZ.Domain.Shared;
using IGBZ.Infrastructure.MongoDb;
using MongoDB.Driver;

namespace IGBZ.Infrastructure.Repositories;

public sealed class TenantScopedMongoRepository<T>(
    MongoDbContext dbContext,
    ITenantContextAccessor tenantContextAccessor)
    : ITenantScopedRepository<T>
    where T : TenantScopedEntity
{
    private readonly IMongoCollection<T> _collection = dbContext.Collection<T>();

    public async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = TenantFilter() & Builders<T>.Filter.Eq(entity => entity.Id, id);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var filter = TenantFilter() & Builders<T>.Filter.Where(predicate);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var filter = TenantFilter();
        if (predicate is not null)
        {
            filter &= Builders<T>.Filter.Where(predicate);
        }

        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        EnsureTenantMatches(entity);
        await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task ReplaceAsync(T entity, CancellationToken cancellationToken = default)
    {
        EnsureTenantMatches(entity);
        var filter = TenantFilter() & Builders<T>.Filter.Eq(item => item.Id, entity.Id);
        var result = await _collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
        if (result.MatchedCount == 0)
        {
            throw new DomainException($"{typeof(T).Name} was not found for current tenant.");
        }
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = TenantFilter() & Builders<T>.Filter.Eq(entity => entity.Id, id);
        await _collection.DeleteOneAsync(filter, cancellationToken);
    }

    private FilterDefinition<T> TenantFilter()
    {
        var tenantId = tenantContextAccessor.RequiredTenantId;
        return Builders<T>.Filter.Eq(entity => entity.TenantId, tenantId);
    }

    private void EnsureTenantMatches(T entity)
    {
        var tenantId = tenantContextAccessor.RequiredTenantId;
        if (!string.Equals(entity.TenantId, tenantId, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("Attempted to persist an entity outside the current tenant scope.");
        }
    }
}
