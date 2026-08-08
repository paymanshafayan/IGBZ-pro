namespace IGBZ.Infrastructure.Tests;

using IGBZ.Infrastructure;
using IGBZ.Infrastructure.Mongo;
using Microsoft.Extensions.DependencyInjection;
using Mongo2Go;
using Xunit;

/// <summary>
/// پایهٔ تست‌های زیرساخت — یک MongoDB واقعی (در-حافظه با Mongo2Go) بالا می‌آورد
/// و DI را با همان تنظیمات Production می‌سازد.
/// </summary>
public abstract class MongoTestBase : IAsyncLifetime
{
    protected MongoDbRunner Runner { get; private set; } = null!;
    protected ServiceProvider Provider { get; private set; } = null!;

    public virtual Task InitializeAsync()
    {
        Runner = MongoDbRunner.Start();

        var services = new ServiceCollection();
        services.AddIgBzInfrastructure(new MongoOptions
        {
            ConnectionString = Runner.ConnectionString,
            DatabaseName = "igbz-test"
        });

        Provider = services.BuildServiceProvider();
        return Task.CompletedTask;
    }

    public virtual Task DisposeAsync()
    {
        Provider?.Dispose();
        Runner?.Dispose();
        return Task.CompletedTask;
    }
}
