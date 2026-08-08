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
  IGBZ.Infrastructure/  MongoDB، Repository های تننت‌محور، JWT، درگاه‌ها
  IGBZ.Api/             Web API واحد + Middleware تننت + Controllers
apps/
  storefront/           فروشگاه تننت (Next.js + React 19)
  admin_app/            اپ ادمین تننت (Flutter)
  customer_app/         اپ مشتریان (Flutter + Deep Link)
tests/
  IGBZ.Domain.Tests/        ماشین‌حالت، Money، موجودی
  IGBZ.Application.Tests/   پایپلاین قیمت، تخفیف، کیف‌پول، کاتالوگ، Provisioning، ادمین
  IGBZ.Infrastructure.Tests/ جداسازی تننت واقعی (Mongo2Go)، end-to-end
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
| 1 | هستهٔ موتور تجارت + چندمستأجری | ✅ |
| 2 | Web API واحد + JWT + Provisioning + درگاه تست | ✅ |
| 3 | Storefront تننت (Next.js) | ✅ |
| 4-5 | Web API ادمین + اپ ادمین Flutter + اپ مشتری Flutter + Deep Link | ✅ |
| 6 | Integration Provider Framework + پرداخت واقعی (pay.ir/BNPL/رمز) | ✅ |
| 7 | دستیار اینستاگرام | ⏳ |
| 8 | مارکت‌پلیس (ترب/دیجی‌کالا/دیوار) + لجستیک | ✅ |
| 9 | استودیوی AI محتوا (عکس/ویدیو/صدا/ترجمه/سئو) | ✅ |
| 10 | LMS (آموزش + امنیت ویدیو) | ✅ |
| 11 | حسابداری/مؤدیان + تخفیف سطح ۳ | ✅ |
| 12 | سخت‌سازی امنیتی (AES، Rate Limit، قفل لاگین) | ✅ |
| 7 | دستیار اینستاگرام | ⏳ (آخرین) |
