# IGBZ API Surface (initial)

همهٔ مسیرها نسخه‌دار هستند و با `/api/v1` شروع می‌شوند.

## OpenAPI

در محیط Development سند OpenAPI در این مسیر منتشر می‌شود:

```text
/openapi/v1.json
```

## Tenant Resolution

به جز مسیرهای Platform و Health، همهٔ درخواست‌ها باید Tenant داشته باشند.

راه‌های فعلی:

```http
X-Tenant-Id: <tenant-id>
```

یا بعد از Login:

```http
Authorization: Bearer <access-token>
```

JWT شامل claim زیر است:

```text
tenantId
```

## Auth API

| Method | Path | توضیح |
|---|---|---|
| POST | `/api/v1/auth/otp/request` | درخواست OTP برای ورود ادمین یا مشتری |
| POST | `/api/v1/auth/otp/verify` | تایید OTP و دریافت JWT |

> در حالت توسعه، کد OTP در پاسخ API با فیلد `developmentCode` برمی‌گردد. در Production باید به SMS Provider متصل و این گزینه خاموش شود.

## Platform API

| Method | Path | توضیح |
|---|---|---|
| GET | `/api/v1/platform/plans` | لیست پلن‌های فعال |
| POST | `/api/v1/platform/plans` | ایجاد پلن |
| GET | `/api/v1/platform/onboarding/subdomains/{subdomain}` | بررسی آزاد بودن زیردامنه |
| POST | `/api/v1/platform/onboarding/provision` | ساخت Tenant، Subscription، Domain Mapping و Owner اولیه |

## Admin API

Admin API با policy زیر محافظت می‌شود:

```text
RequireAuthorization("TenantAdmin")
Roles: TenantOwner یا TenantAdmin
```

| Method | Path | توضیح |
|---|---|---|
| POST | `/api/v1/admin/categories` | ایجاد دسته‌بندی |
| GET | `/api/v1/admin/categories` | لیست دسته‌بندی‌ها |
| POST | `/api/v1/admin/products` | ایجاد محصول و Variantها |
| GET | `/api/v1/admin/products` | لیست محصولات Tenant |
| GET | `/api/v1/admin/products/{id}` | جزئیات محصول |
| POST | `/api/v1/admin/products/{id}/publish` | انتشار محصول |
| POST | `/api/v1/admin/discounts` | ایجاد تخفیف/کوپن |
| GET | `/api/v1/admin/discounts` | لیست تخفیف‌ها |
| GET | `/api/v1/admin/inventory` | لیست موجودی Variantها |
| POST | `/api/v1/admin/inventory/{id}/adjust` | تنظیم موجودی on-hand |
| POST | `/api/v1/admin/integrations` | ایجاد اتصال Provider بیرونی |
| GET | `/api/v1/admin/integrations` | لیست اتصال‌ها |
| POST | `/api/v1/admin/integrations/{connectionId}/sync-jobs` | صف کردن Sync Job |
| GET | `/api/v1/admin/integrations/sync-jobs` | لیست Sync Jobها |
| GET | `/api/v1/admin/orders` | لیست سفارش‌ها |
| GET | `/api/v1/admin/orders/{id}` | جزئیات سفارش |
| POST | `/api/v1/admin/orders/{id}/processing` | تغییر وضعیت به Processing |
| POST | `/api/v1/admin/orders/{id}/shipped` | تغییر وضعیت به Shipped |
| POST | `/api/v1/admin/orders/{id}/delivered` | تغییر وضعیت به Delivered |
| POST | `/api/v1/admin/orders/{id}/cancel` | لغو سفارش |
| POST | `/api/v1/admin/orders/{id}/return` | برگشت سفارش |
| POST | `/api/v1/admin/orders/{id}/refund` | Refund سفارش |

## Storefront API

| Method | Path | توضیح |
|---|---|---|
| GET | `/api/v1/storefront/products` | لیست محصولات منتشرشده |
| GET | `/api/v1/storefront/products/{slug}` | جزئیات محصول منتشرشده |
| POST | `/api/v1/storefront/checkout/orders` | ایجاد سفارش و رزرو موجودی |
| GET | `/api/v1/storefront/orders/{id}` | مشاهده سفارش |

## Payments API

| Method | Path | توضیح |
|---|---|---|
| POST | `/api/v1/payments/intents` | ایجاد PaymentIntent با درگاه mock |
| POST | `/api/v1/payments/mock-callback` | شبیه‌سازی callback موفق/ناموفق درگاه |
