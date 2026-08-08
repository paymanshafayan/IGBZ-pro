# ARCHITECTURE — IGBZ بومی

> سند مرجع معماری پلتفرم بومی IGBZ. این نسخه، اجرای «مسیر بومی» از سند
> `ARCHITECTURE-NATIVE-v2` است که پیش‌تر در مخزن `IGBZ-NopCommerce` تدوین شده بود
> و اکنون به‌عنوان پروژهٔ مستقل (بدون وابستگی به nopCommerce) روی این مخزن پیاده می‌شود.

## ۱. استک

| لایه | انتخاب | دلیل |
|---|---|---|
| بک‌اند | .NET 9 / C# — Clean + Vertical Slice | تداوم دانش تیم |
| دیتابیس | MongoDB 7+ (Replica Set از روز اول) | سند تودرتوی طبیعی Order/Product؛ `findOneAndUpdate` اتمیک |
| کش/صف | Redis + RabbitMQ + Hangfire (Mongo Storage) | کش/Rate-Limit/Lock؛ رویدادهای Async؛ Jobهای زمان‌بندی‌شده |
| فرانت | Next.js (App Router) | سایت مادر + Storefront |
| موبایل | Flutter (۲ اپ) | ادمین، مشتریان |
| VOD | آروان‌کلاد، HLS + Signed URL | فاز ۱۰ (LMS) |
| CI/CD | GitHub Actions → Docker | از روز اول |

## ۲. چندمستأجری (قانون سخت)

- هر Document در هر Collection فیلد اجباری `tenantId` دارد.
- `ITenantScopedRepository<TEntity>` (با `where TEntity : class, ITenantEntity`) — هیچ Query بدون
  `TenantContext` پذیرفته نیست: Runtime Guard که **Exception پرتاب می‌کند** (نه نتیجهٔ خالی).
- `tenantId` از JWT Claim (موبایل) یا Host Header (وب) تزریق می‌شود.
- سایت مادر یک «شبه‌تننت» با `tenantId = "platform"` است.

## ۳. مدل داده (خلاصه)

`tenants`, `products` (variants + رزرو اتمیک موجودی), `orders` (ماشین‌حالت + statusHistory),
`paymentTransactionLedger` (Unique روی trackingNumber — ضد Replay),
`walletLedger` (موجودی = SUM), `integrationCredentials` (AES در حالت سکون),
`tenantPlans`, `tenantStoreSubscriptions`.

## ۴. موتور سفارش

- ماشین‌حالت: `Pending → Paid → Processing → Shipped → Delivered` (و شاخه‌های Cancel/Return).
- تغییر وضعیت فقط از طریق متدهای Aggregate (`MarkAsPaid()` و…).
- پایپلاین قیمت: `SubTotal → Discount → Tax → Shipping → GrandTotal` — هر مرحله `ICalculator<TIn,TOut>`.
- موجودی: رزرو اتمیک با `findOneAndUpdate` روی شرط `stock - reserved >= qty`؛ آزادسازی با Job زمان‌بندی‌شده.

## ۵. پرداخت و دفترکل (قانون سخت Verify)

`VerifyAsync` فقط با این سه شرط `IsSuccess=true` است:
1) فراخوانی HTTP واقعی به Endpoint تایید PSP؛
2) پاسخ 2xx؛
3) مبلغ تاییدشده == مبلغ سفارش در `paymentTransactionLedger`.
تایید دوم روی همان `trackingNumber` با `state=VerifiedSuccess` قبلی، Idempotent است (پول دوباره واریز نمی‌شود).

## ۶. موتور تخفیف

سطح ۲ در فاز ۱: کوپن (درصدی/مبلغ ثابت، حداقل سبد، انقضا، سقف استفاده، عدم ترکیب).
معماری از روز اول برای سطح ۳ باز است: پایپلاین `List<AppliedDiscount>` می‌گیرد (نه مقدار تکی).

## ۷. قواعد مهندسی الزامی (از ممیزی کد مرجع)

1. **بدون موفقیت بی‌دلیل:** هیچ متد Async بدون فراخوانی واقعی و بررسی کد وضعیت، `true` برنگرداند.
2. **داده از منبع واقعی:** هر Feed/گزارش از Query واقعی ساخته شود.
3. **کامپایل قبل از ادعا:** CI از روز اول (`build.yml`) هر commit را build/test می‌کند.
4. **تصادفی رمزنگارانه:** هر مقدار امنیتی از `RandomNumberGenerator`.
5. **موجودی محاسبه‌شده:** کیف‌پول/اعتبار = SUM دفترکل؛ هرگز فیلد «موجودی فعلی» ذخیره نشود.
6. **Idempotency مالی:** هر عملیات مالی شناسهٔ یکتا دارد (Unique Index سطح DB).

## ۸. فازبندی

| فاز | عنوان | وضعیت |
|---|---|---|
| 0 | زیرساخت (CI، docker-compose، ساختار راه‌حل) | ✅ |
| 1 | هستهٔ موتور تجارت + چندمستأجری (مدل داده، Order Engine، Discount سطح ۲، Ledgerها) | 🟡 |
| 2 | Web API واحد + Middleware تننت + JWT + Provisioning + درگاه تست | ⏳ |
| 3 | Storefront تننت (Next.js) | ⏳ |
| 4-5 | اپ‌های Flutter ادمین/مشتری + Deep Link | ⏳ |
| 6 | Integration Provider Framework + پرداخت گسترده (pay.ir/BNPL/رمز) | ⏳ |
| 7 | دستیار اینستاگرام | ⏳ |
| 8 | مارکت‌پلیس + لجستیک | ⏳ |
| 9 | استودیوی AI محتوا | ⏳ |
| 10 | LMS | ⏳ |
| 11-12 | حسابداری/مؤدیان + سخت‌سازی و Production | ⏳ |
