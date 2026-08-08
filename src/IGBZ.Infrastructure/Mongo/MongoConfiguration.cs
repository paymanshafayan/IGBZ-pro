namespace IGBZ.Infrastructure.Mongo;

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

/// <summary>
/// پیکربندی نگاشت Bson — یک‌بار در فرایند: نام‌های camelCase برای عناصر و نادیده‌گرفتن فیلدهای اضافه.
/// </summary>
public static class MongoConfiguration
{
    private static readonly object Sync = new();
    private static bool _configured;

    public static void Configure()
    {
        if (_configured)
            return;

        lock (Sync)
        {
            if (_configured)
                return;

            var pack = new ConventionPack();
            pack.Add(new CamelCaseElementNameConvention());
            pack.Add(new IgnoreExtraElementsConvention(true));
            ConventionRegistry.Register("igbz", pack, _ => true);

            _configured = true;
        }
    }
}
