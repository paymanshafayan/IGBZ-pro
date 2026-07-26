namespace IGBZ.Infrastructure.MongoDb;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; init; } = "mongodb://igbz:igbz-dev-password@localhost:27017";
    public string DatabaseName { get; init; } = "igbz_dev";
}
