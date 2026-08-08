namespace IGBZ.Infrastructure.Repositories;

using System.Linq.Expressions;
using IGBZ.Application.Abstractions;
using IGBZ.Infrastructure.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;

/// <summary>Repository ساده برای موجودیت‌های سطح پلتفرم (بدون tenantId).</summary>
public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    private readonly IMongoCollection<TEntity> _collection;

    public Repository(MongoDbContext dbContext)
    {
        _collection = dbContext.GetCollection<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(Builders<TEntity>.Filter.Eq("_id", id)).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var items = await _collection.Find(Builders<TEntity>.Filter.Where(predicate)).ToListAsync(cancellationToken);
        return items;
    }

    public async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _collection.Find(Builders<TEntity>.Filter.Where(predicate)).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity is IGBZ.Domain.Common.Entity baseEntity && string.IsNullOrWhiteSpace(baseEntity.Id))
            baseEntity.Id = ObjectId.GenerateNewId().ToString();

        await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity is IGBZ.Domain.Common.Entity baseEntity && string.IsNullOrWhiteSpace(baseEntity.Id))
            throw new InvalidOperationException("شناسهٔ موجودیت برای به‌روزرسانی الزامی است.");

        await _collection.ReplaceOneAsync(
            Builders<TEntity>.Filter.Eq("_id", ((IGBZ.Domain.Common.Entity)entity).Id),
            entity,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await _collection.DeleteOneAsync(Builders<TEntity>.Filter.Eq("_id", id), cancellationToken);
    }
}
