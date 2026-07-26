using System.Linq.Expressions;
using IGBZ.Application.Abstractions;
using IGBZ.Domain.Shared;
using IGBZ.Infrastructure.MongoDb;
using MongoDB.Driver;

namespace IGBZ.Infrastructure.Repositories;

public sealed class PlatformMongoRepository<T>(MongoDbContext dbContext) : IPlatformRepository<T>
    where T : Entity
{
    private readonly IMongoCollection<T> _collection = dbContext.Collection<T>();

    public async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => await _collection.Find(entity => entity.Id == id).FirstOrDefaultAsync(cancellationToken);

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await _collection.Find(predicate).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => predicate is null
            ? await _collection.Find(Builders<T>.Filter.Empty).ToListAsync(cancellationToken)
            : await _collection.Find(predicate).ToListAsync(cancellationToken);

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);

    public async Task ReplaceAsync(T entity, CancellationToken cancellationToken = default)
    {
        var result = await _collection.ReplaceOneAsync(item => item.Id == entity.Id, entity, cancellationToken: cancellationToken);
        if (result.MatchedCount == 0)
        {
            throw new DomainException($"{typeof(T).Name} was not found.");
        }
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        => await _collection.DeleteOneAsync(entity => entity.Id == id, cancellationToken);
}
