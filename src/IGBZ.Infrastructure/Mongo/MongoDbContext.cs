namespace IGBZ.Infrastructure.Mongo;

using MongoDB.Driver;

/// <summary>دسترسی به دیتابیس و Collection ها.</summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(MongoOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            throw new ArgumentException("ConnectionString مونگو الزامی است.", nameof(options));

        MongoConfiguration.Configure();

        var client = new MongoClient(options.ConnectionString);
        _database = client.GetDatabase(options.DatabaseName);
    }

    /// <summary>نام Collection از نام تایپ (مثل "Order" → "Order").</summary>
    public IMongoCollection<TEntity> GetCollection<TEntity>()
    {
        return _database.GetCollection<TEntity>(typeof(TEntity).Name);
    }
}
