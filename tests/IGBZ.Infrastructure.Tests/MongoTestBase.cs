namespace IGBZ.Infrastructure.Tests;

using IGBZ.Infrastructure;
using IGBZ.Infrastructure.Auth;
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

    private static readonly JwtOptions TestJwtOptions = new()
    {
        SigningKey = "test-signing-key-0123456789abcdefghijklmnopqrstuv",
        Issuer = "igbz-test",
        Audience = "igbz-test-apps"
    };

    public virtual Task InitializeAsync()
    {
        Runner = MongoDbRunner.Start();

        var services = new ServiceCollection();
        services.AddIgBzInfrastructure(new MongoOptions
        {
            ConnectionString = Runner.ConnectionString,
            DatabaseName = "igbz-test"
        }, TestJwtOptions);

        Provider = services.BuildServiceProvider();
        return Task.CompletedTask;
    }

    /// <summary>بازسازی Provider با سرویس‌های اضافی (برای کلاس‌های فرزند).</summary>
    protected void RebuildProvider(params Action<ServiceCollection>[] configure)
    {
        var services = new ServiceCollection();
        services.AddIgBzInfrastructure(new MongoOptions
        {
            ConnectionString = Runner.ConnectionString,
            DatabaseName = "igbz-test"
        }, TestJwtOptions);

        foreach (var action in configure)
            action(services);

        Provider.Dispose();
        Provider = services.BuildServiceProvider();
    }

    public virtual Task DisposeAsync()
    {
        Provider?.Dispose();
        Runner?.Dispose();
        return Task.CompletedTask;
    }
}
