# IGBZ — پلتفرم بومی «دستیار کسب‌وکارهای اینستاگرامی»

پلتفرم بومی (بدون وابستگی به nopCommerce) بر اساس سند معماری
[`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

| لایه | استک |
|---|---|
| بک‌اند | .NET 9 / C# — Clean Architecture |
| دیتابیس | MongoDB 7+ (Replica Set — الزامی برای Transactions) |
| کش/صف | Redis + RabbitMQ + Hangfire |
| فرانت | Next.js (فاز ۳ به بعد) |
| موبایل | Flutter (فاز ۴ به بعد) |

## ساختار

```
src/
  IGBZ.Domain/          موجودیت‌ها، Value Objects، Aggregate ها، ماشین‌حالت سفارش
  IGBZ.Application/     اینترفیس‌ها، پایپلاین قیمت‌گذاری، TenantContext، موتور تخفیف
  IGBZ.Infrastructure/  MongoDB، Repository های تننت‌محور
  IGBZ.Api/             Web API واحد (فاز ۲)
tests/
  IGBZ.Domain.Tests/        ماشین‌حالت، Money، موجودی
  IGBZ.Application.Tests/   پایپلاین قیمت، تخفیف، جداسازی چندمستأجری
  IGBZ.Infrastructure.Tests/
```

## اجرای محلی

```bash
# ۱) زیرساخت (MongoDB Replica Set + Redis + RabbitMQ)
docker compose -f docker-compose.dev.yml up -d

# ۲) بیلد و تست
dotnet restore IGBZ.sln
dotnet build IGBZ.sln
dotnet test IGBZ.sln
```

## CI

فایل workflow در `ci/build.yml` قرار دارد (build + test روی هر push). چون اپ گیت‌هابِ
ایجادکنندهٔ این ریپو هنوز دسترسی `Workflows` ندارد، برای فعال‌سازی آن را به
`.github/workflows/build.yml` منتقل کنید (یا دسترسی Workflows اپ را فعال کنید تا خودمان انجام دهیم).

## وضعیت فازها

| فاز | عنوان | وضعیت |
|---|---|---|
| 0 | زیرساخت (CI، docker-compose، ساختار راه‌حل) | ✅ |
| 1 | هستهٔ موتور تجارت + چندمستأجری | 🟡 در حال انجام |
| 2+ | API، سایت مادر، Storefront، اپ‌ها | ⏳ |
