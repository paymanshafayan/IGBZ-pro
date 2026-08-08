namespace IGBZ.Infrastructure.Mongo;

/// <summary>تنظیمات اتصال MongoDB.</summary>
public class MongoOptions
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "igbz";
}
