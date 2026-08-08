# Storefront تننت (Next.js)

فروشگاه اینترنتی هر تننت — با App Router و React 19. از طریق پراکسی `/api` به بک‌اند IGBZ وصل می‌شود.

## اجرای محلی

```bash
# ۱) بک‌اند IGBZ (نیازمند MongoDB)
cd ../../src/IGBZ.Api
dotnet run            # روی http://localhost:5000

# ۲) Storefront
cd ../../apps/storefront
npm install
npm run dev           # روی http://localhost:3000
```

## پیکربندی

- `IGBZ_API_BASE` — آدرس بک‌اند (پیش‌فرض `http://localhost:5000`؛ در dev به‌صورت پراکسی استفاده می‌شود)
- `NEXT_PUBLIC_TENANT_ID` — برای توسعهٔ محلی (مثلاً `modstyle`)؛ اگر خالی باشد، تننت از Host مرورگر تشخیص داده می‌شود

## صفحات

| مسیر | توضیح |
|---|---|
| `/` | گرید محصولات (دادهٔ واقعی از `GET /api/catalog/products`) |
| `/product/[slug]` | جزئیات محصول + انتخاب واریانت + افزودن به سبد |
| `/cart` | سبد خرید (localStorage) |
| `/checkout` | ثبت سفارش (رزرو اتمیک موجودی + محاسبهٔ قیمت) |

## نکات

- پرداخت آنلاین سفارش در فاز ۶ (اتصال درگاه واقعی pay.ir) وصل می‌شود؛ فعلاً سفارش با وضعیت Pending ثبت و موجودی رزرو می‌شود.
- احراز هویت مشتری در Storefront در فاز ۴-۵ (اپ مشتری + ورود) تکمیل می‌شود؛ فعلاً شناسهٔ مهمان پذیرفته می‌شود.
