# IGBZ-pro

IGBZ یک پلتفرم SaaS بومی برای فروشگاه‌ساز کسب‌وکارهای اینستاگرامی است. این نسخه شروع پیاده‌سازی معماری Native است و دیگر به nopCommerce وابسته نیست.

## وضعیت فعلی

پیاده‌سازی اولیه شامل موارد زیر است:

- Solution با .NET 9 / C#
- معماری لایه‌ای Domain / Application / Infrastructure / API
- MongoDB به عنوان دیتابیس اصلی
- Multi-tenancy اجباری با `X-Tenant-Id`، JWT Claim یا دامنه
- Repository چندمستأجری `ITenantScopedRepository<T>`
- Tenant provisioning اولیه همراه با ساخت Tenant Owner
- Auth اولیه با OTP و JWT
- OpenAPI در محیط Development
- Catalog: محصول، Variant، دسته‌بندی
- Inventory: موجودی Variant-based و رزرو Atomic
- Order Engine: ماشین‌حالت سفارش و endpointهای تغییر وضعیت
- Pricing Pipeline: Subtotal، Discount، VAT، Shipping، GrandTotal
- Discount MVP سطح ۲: درصدی/مبلغ ثابت + کوپن
- Payment abstraction و Mock Gateway
- Admin API و Storefront API اولیه
- Integration Provider Framework اولیه + Mongo-backed Job Worker
- تست‌های واحد برای Order State Machine و Pricing

## پیش‌نیازها

- .NET SDK 9
- Docker / Docker Compose
- MongoDB، یا اجرای MongoDB از طریق docker-compose

> در محیط Arena فعلی دستور `dotnet` نصب نبود و دانلود SDK هم به‌دلیل خطای TLS/شبکه ممکن نشد؛ بنابراین Build/Test واقعی در همین محیط اجرا نشد. پروژه برای .NET 9 آماده شده است.

## اجرا

ابتدا MongoDB را بالا بیاورید:

```bash
docker compose up -d
```

سپس API را اجرا کنید:

```bash
dotnet restore
dotnet run --project src/IGBZ.Api
```

Health check:

```bash
curl http://localhost:5000/health
```

OpenAPI در Development:

```text
http://localhost:5000/openapi/v1.json
```

## Tenant Resolution

برای مسیرهای Tenant-scoped یکی از این دو روش را استفاده کنید:

```http
X-Tenant-Id: <tenantId>
```

یا برای Admin API بعد از ورود:

```http
Authorization: Bearer <accessToken>
```

## جریان تست دستی MVP

### 1. ایجاد Plan

```bash
curl -X POST http://localhost:5000/api/v1/platform/plans \
  -H "Content-Type: application/json" \
  -d '{"code":"starter","title":"Starter","monthlyPrice":990000,"currency":"IRR"}'
```

### 2. Provision کردن Tenant

```bash
curl -X POST http://localhost:5000/api/v1/platform/onboarding/provision \
  -H "Content-Type: application/json" \
  -d '{"storeName":"Demo Store","subdomain":"demo","planCode":"starter","adminEmail":"owner@example.com","adminPhone":"09120000000"}'
```

خروجی شامل `tenantId` است. Provisioning علاوه بر Tenant، یک Owner اولیه با شماره موبایل `adminPhone` می‌سازد.

### 3. ورود ادمین با OTP

درخواست OTP:

```bash
curl -X POST http://localhost:5000/api/v1/auth/otp/request \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: <tenantId>" \
  -d '{"phoneNumber":"09120000000","purpose":"AdminLogin"}'
```

در حالت توسعه، پاسخ شامل `developmentCode` است. سپس تایید OTP:

```bash
curl -X POST http://localhost:5000/api/v1/auth/otp/verify \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: <tenantId>" \
  -d '{"challengeId":"<challengeId>","phoneNumber":"09120000000","purpose":"AdminLogin","code":"<developmentCode>"}'
```

خروجی شامل `accessToken` است. برای Admin API از این هدر استفاده کنید:

```http
Authorization: Bearer <accessToken>
```

### 4. ایجاد محصول از Admin API

```bash
curl -X POST http://localhost:5000/api/v1/admin/products \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <accessToken>" \
  -d '{
    "name":"T-Shirt",
    "slug":"t-shirt",
    "description":"Demo product",
    "type":"Physical",
    "categoryIds":[],
    "publishImmediately":true,
    "variants":[
      {"sku":"TS-BLK-M","name":"Black / M","price":500000,"currency":"IRR","attributes":{"color":"black","size":"M"},"initialStock":10,"isDefault":true}
    ]
  }'
```

### 5. مشاهده محصولات Storefront

```bash
curl http://localhost:5000/api/v1/storefront/products \
  -H "X-Tenant-Id: <tenantId>"
```

### 6. ایجاد سفارش

```bash
curl -X POST http://localhost:5000/api/v1/storefront/checkout/orders \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: <tenantId>" \
  -d '{
    "customer":{"customerId":null,"fullName":"Ali Ahmadi","phoneNumber":"09120000000","email":null},
    "shippingAddress":{"fullName":"Ali Ahmadi","phoneNumber":"09120000000","province":"Tehran","city":"Tehran","streetLine":"Street 1","postalCode":null,"nationalCode":null},
    "items":[{"productId":"<productId>","variantId":"<variantId>","quantity":1}],
    "couponCode":null,
    "shippingAmount":50000
  }'
```

### 7. ایجاد PaymentIntent و Callback تستی

```bash
curl -X POST http://localhost:5000/api/v1/payments/intents \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: <tenantId>" \
  -d '{"orderId":"<orderId>","callbackUrl":"http://localhost:5000/api/v1/payments/mock-callback"}'
```

سپس با مقادیر خروجی:

```bash
curl -X POST http://localhost:5000/api/v1/payments/mock-callback \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: <tenantId>" \
  -d '{"paymentIntentId":"<paymentIntentId>","gatewayReference":"<gatewayReference>","amount":<amount>,"currency":"IRR","isSuccessful":true}'
```

### 8. تغییر وضعیت سفارش از Admin API

```bash
curl -X POST http://localhost:5000/api/v1/admin/orders/<orderId>/processing \
  -H "Authorization: Bearer <accessToken>"
```

مسیرهای موجود:

```text
/orders/{id}/processing
/orders/{id}/shipped
/orders/{id}/delivered
/orders/{id}/cancel
/orders/{id}/return
/orders/{id}/refund
```

## تست‌ها

```bash
dotnet test
```

## اسناد

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- [docs/DECISIONS.md](docs/DECISIONS.md)
- [docs/API.md](docs/API.md)
