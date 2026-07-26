using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace IGBZ.Infrastructure.MongoDb;

public sealed class MongoDbContext
{
    private static int _conventionsRegistered;

    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        RegisterConventionsOnce();
        var client = new MongoClient(options.Value.ConnectionString);
        Database = client.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoDatabase Database { get; }

    public IMongoCollection<T> Collection<T>() => Database.GetCollection<T>(MongoCollectionNames.For<T>());

    private static void RegisterConventionsOnce()
    {
        if (Interlocked.Exchange(ref _conventionsRegistered, 1) == 1)
        {
            return;
        }

        var pack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new EnumRepresentationConvention(BsonType.String),
            new IgnoreExtraElementsConvention(true)
        };

        ConventionRegistry.Register("igbz-conventions", pack, _ => true);
    }
}
