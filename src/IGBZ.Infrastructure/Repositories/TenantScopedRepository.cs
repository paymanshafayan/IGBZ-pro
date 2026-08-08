namespace IGBZ.Infrastructure.Repositories;

using System.Linq.Expressions;
using IGBZ.Application.Abstractions;
using IGBZ.Application.Tenancy;
using IGBZ.Domain.Common;
using IGBZ.Infrastructure.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;

/// <summary>
/// پیاده‌سازی تننت‌محور — همهٔ کوئری‌ها به‌اجبار با فیلتر <c>TenantId</c> ترکیب می‌شوند و بدون
/// بافت تننت، <see cref="TenantRequiredException"/> پرتاب می‌شود (سند بخش ۵).
/// </summary>
public class TenantScopedRepository<TEntity> : ITenantScopedRepository<TEntity>
    where TEntity : class, ITenantEntity
{
    private readonly IMongoCollection<TEntity> _collection;
    private readonly ITenantContext _tenantContext;

    public TenantScopedRepository(MongoDbContext dbContext, ITenantContext tenantContext)
    {
        _collection = dbContext.GetCollection<TEntity>();
        _tenantContext = tenantContext;
    }

    protected string RequireTenant() =>
        _tenantContext.TenantId ?? throw new TenantRequiredException(typeof(TEntity).Name);

    protected static FilterDefinition<TEntity> TenantFilter(string tenantId) =>
        Builders<TEntity>.Filter.Eq(e => e.TenantId, tenantId);

    protected IMongoCollection<TEntity> Collection => _collection;
    protected ITenantContext TenantContext => _tenantContext;

    public virtual async Task<TEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenant();
        var filter = Builders<TEntity>.Filter.And(
            Builders<TEntity>.Filter.Eq(e => e.Id, id),
            TenantFilter(tenantId));

        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenant();
        var filter = Builders<TEntity>.Filter.And(TenantFilter(tenantId), Builders<TEntity>.Filter.Where(predicate));

        var items = await _collection.Find(filter).ToListAsync(cancellationToken);
        return items;
    }

    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenant();
        var filter = Builders<TEntity>.Filter.And(TenantFilter(tenantId), Builders<TEntity>.Filter.Where(predicate));

        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenant();
        var filter = Builders<TEntity>.Filter.And(TenantFilter(tenantId), Builders<TEntity>.Filter.Where(predicate));

        return (int)await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public virtual async Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenant();

        if (string.IsNullOrWhiteSpace(entity.TenantId))
            entity.TenantId = tenantId;
        else if (entity.TenantId != tenantId)
            throw new InvalidOperationException(
                $"TenantId موجودیت «{typeof(TEntity).Name}» با بافت تننت جاری ناسازگار است.");

        if (string.IsNullOrWhiteSpace(entity.Id))
            entity.Id = ObjectId.GenerateNewId().ToString();

        await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenant();
        if (string.IsNullOrWhiteSpace(entity.Id))
            throw new InvalidOperationException("شناسهٔ موجودیت برای به‌روزرسانی الزامی است.");

        var filter = Builders<TEntity>.Filter.And(
            Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id),
            TenantFilter(tenantId));

        await _collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
    }

    public virtual async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenant();
        var filter = Builders<TEntity>.Filter.And(
            Builders<TEntity>.Filter.Eq(e => e.Id, id),
            TenantFilter(tenantId));

        await _collection.DeleteOneAsync(filter, cancellationToken);
    }
}
