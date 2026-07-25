# IGBZ Native Architecture

این فایل نسخهٔ اجرایی‌شدهٔ سند معماری Native است. تصمیم اصلی پروژه: جدایی کامل از nopCommerce و ساخت یک پلتفرم SaaS بومی، Headless و چندمستأجری با .NET و MongoDB.

## اصول غیرقابل نقض

1. هر دادهٔ عملیاتی Tenant باید `tenantId` داشته باشد.
2. دسترسی به دادهٔ Tenant فقط از طریق `ITenantScopedRepository<T>` مجاز است.
3. Tenant از JWT Claim، هدر کنترل‌شدهٔ `X-Tenant-Id`، یا دامنه/زیردامنه استخراج می‌شود.
4. موتور سفارش Aggregateمحور است؛ تغییر وضعیت سفارش فقط از متدهای دامنه مثل `MarkAsPaid`، `Cancel`، `MarkAsShipped` انجام می‌شود.
5. محاسبه سفارش به صورت پایپ‌لاین مستقل و تست‌پذیر انجام می‌شود:

```text
SubTotal -> Discounts -> VAT -> Shipping -> GrandTotal
```

6. موجودی Variant-based است و رزرو موجودی با عملیات Atomic روی MongoDB انجام می‌شود.
7. پرداخت پشت `IPaymentGatewayService` قرار دارد؛ پیاده‌سازی فعلی `mock` است.
8. nopCommerce فقط مرجع مفهومی برای Edge Caseهاست و هیچ کدی از آن کپی نشده است.

## وضعیت پیاده‌سازی فعلی

| فاز | عنوان | وضعیت |
|---|---|---|
| 0 | زیرساخت هماهنگی و سند معماری | انجام‌شده در قالب docs/ARCHITECTURE.md و docs/DECISIONS.md |
| 1 | هستهٔ موتور تجارت + چندمستأجری | شروع شده: Tenancy، Identity/OTP/JWT، Catalog، Inventory، Order، Pricing، Payment Mock |
| 2 | سایت مادر + ویزارد + Provisioning + پرداخت پایه | API اولیه Provisioning و Plan اضافه شده؛ Tenant Owner ساخته می‌شود؛ فرانت هنوز اضافه نشده |
| 3 | فروشگاه تننت | Storefront API اولیه اضافه شده |
| 4 | Web API ادمین | Admin API محصول/دسته/تخفیف/موجودی/سفارش/Integration اضافه شده و با JWT محافظت می‌شود |
| 5+ | Flutter، Marketplace، Instagram، LMS، AI، Accounting | هنوز پیاده‌سازی نشده؛ چارچوب اولیه Integration و Job Worker آماده است |

## ساختار Solution

```text
src/
  IGBZ.Domain/          # موجودیت‌ها و منطق دامنه
  IGBZ.Application/     # سرویس‌های کاربردی، DTOها، اینترفیس‌ها
  IGBZ.Infrastructure/  # MongoDB، Repository، Inventory atomic، Payment mock، JWT، Job Worker
  IGBZ.Api/             # Minimal API، Middlewareها، Endpointها
tests/
  IGBZ.UnitTests/       # تست‌های دامنه و Pricing
```

## Multi-Tenancy

- `TenantResolutionMiddleware` پیش از Endpointها اجرا می‌شود.
- مسیرهای `/api/v1/platform/*` با Tenant ویژهٔ `platform` اجرا می‌شوند.
- سایر مسیرها باید Tenant داشته باشند.
- در توسعهٔ محلی ساده‌ترین راه ارسال هدر زیر است:

```http
X-Tenant-Id: <tenant-id>
```

- بعد از OTP Login، JWT شامل claim `tenantId` است و برای Admin API کافی است.

## Identity / Auth

- ورود Passwordless با OTP پیاده‌سازی شده است.
- Provisioning یک Owner اولیه برای Tenant می‌سازد.
- Admin API با Policy نقش‌های `TenantOwner` و `TenantAdmin` محافظت می‌شود.
- در Development کد OTP در پاسخ API برمی‌گردد؛ در Production باید SMS Provider اضافه و این رفتار غیرفعال شود.

## Collections MongoDB

- `tenants`
- `tenant_plans`
- `tenant_subscriptions`
- `store_domain_mappings`
- `users`
- `otp_challenges`
- `products`
- `categories`
- `inventory_items`
- `stock_reservations`
- `orders`
- `discounts`
- `payment_intents`
- `payment_transactions`
- `integration_connections`
- `integration_sync_jobs`

## Integration Jobs

چارچوب اولیهٔ Integration Provider اضافه شده است:

- ذخیره اتصال Provider برای هر Tenant
- صف کردن Sync Job
- Worker مبتنی بر MongoDB برای پردازش اولیه Jobها

در حال حاضر Worker یک runner no-op دارد و Jobها را به `Succeeded` می‌برد. در فازهای بعدی باید provider-specific runnerها برای ترب، دیجی‌کالا، پستکس، حسابداری، AI و غیره اضافه شوند.

## تصمیم‌های باز باقی‌مانده

- انتخاب نهایی فرانت‌اند وب؛ پیشنهاد فعلی Next.js است.
- انتخاب Job Queue نهایی برای مقیاس بالا؛ فعلاً Mongo-backed Worker کافی است.
- پرداخت ارزی/بین‌المللی باید جداگانه از نظر حقوقی و تحریم بررسی شود.
- SMS/OTP Provider واقعی باید جایگزین development code شود.
