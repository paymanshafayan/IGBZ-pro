using IGBZ.Application;
using IGBZ.Infrastructure;
using IGBZ.Infrastructure.Mongo;

var builder = WebApplication.CreateBuilder(args);

// ── زیرساخت (MongoDB) ──
var mongoOptions = new MongoOptions
{
    ConnectionString = builder.Configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017",
    DatabaseName = builder.Configuration["Mongo:Database"] ?? "igbz"
};
builder.Services.AddIgBzInfrastructure(mongoOptions);

// ── لایهٔ اپلیکیشن (موتور تجارت) ──
builder.Services.AddIgBzApplication();

// فاز ۲: احراز هویت JWT، Middleware تننت، Provisioning و Controller ها اضافه می‌شوند.

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "igbz-api", utc = DateTime.UtcNow }));

app.Run();

// برای تست‌های Integration در آینده
public partial class Program;
