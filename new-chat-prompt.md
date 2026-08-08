# پرامپت چت جدید — اعمال و پوش همهٔ فازهای IGBZ-NopCommerce

## نقش و بستر کار
تو عامل کدنویسی Arena هستی و این چت روی ریپوی **`paymanshafayan/IGBZ-NopCommerce`** (شاخهٔ اصلی `main`) تنظیم شده است. این ریپو شامل ۴ پلاگین برای nopCommerce 4.90.6 است (بدون سورس خود nopCommerce؛ فقط پوشهٔ `Plugins/` و ۶ سند فارسی).

## پیش‌زمینه (مهم)
در یک چت قبلی (که روی ریپوی `IGBZ-pro` ست شده بود) این پروژه کامل بازبینی و چند فاز توسعه انجام شد. چون آن چت به `IGBZ-NopCommerce` دسترسی push نداشت، همهٔ تغییرات به‌صورت **فایل‌های پچ** در ریپوی `IGBZ-pro` (برنچ `arena/019fdead-igbz-pro`) ذخیره شدند. وظیفهٔ تو: **همهٔ پچ‌ها را به ترتیبِ دقیقِ زیر اعمال، کامیت و پوش کن.**

## فایل‌های پچ (در ریپوی `IGBZ-pro` برنچ `arena/019fdead-igbz-pro`)
```
igbz-nopcommerce-fix-round1.patch                    ← اصلاحات دور اول (رفع خطاهای کامپایل، حلقهٔ پرداخت، امنیت)
igbz-nopcommerce-phase1-payir-checklist.patch        ← فاز ۱: درگاه pay.ir (رسمی) + چک‌لیست «فروشگاه‌ت رو بترکون»
igbz-nopcommerce-phase3-virtual-tryon.patch          ← فاز ۳: پرو لباس AI با IDM-VTON (هیبرید محلی/ابری)
igbz-nopcommerce-phase3-tryon-quality-tips.patch     ← فاز ۳+ : نکات کیفیت عکس
igbz-nopcommerce-phase4-manus-manychat-guide.patch   ← فاز ۴: راهنمای گام‌به‌گام Manus/ManyChat/اینفلوئنسر
igbz-nopcommerce-phase2-bnpl.patch                   ← فاز ۲: BNPL دیجی‌پی (رسمی) + اسنپ‌پی (مرجع NopPlus)
igbz-nopcommerce-checklist-admin-action.patch        ← دکمهٔ «تنظیم در پنل» به بخش API Key در چک‌لیست
```

## گام‌های اجرا

### ۱. آماده‌سازی و بررسی دسترسی
```bash
git clone https://github.com/paymanshafayan/IGBZ-NopCommerce.git
cd IGBZ-NopCommerce
git log --oneline -1            # آخرین کامیت origin/main
gh api repos/paymanshafayan/IGBZ-NopCommerce --jq .permissions   # push باید true باشد
```

### ۲. گرفتن فایل‌های پچ
```bash
cd /tmp && rm -rf igbz-pro-src
git clone --depth 1 -b arena/019fdead-igbz-pro https://github.com/paymanshafayan/IGBZ-pro.git igbz-pro-src
ls igbz-pro-src/*.patch
```
- اگر `IGBZ-pro` خصوصی است و دسترسی نداشتی، از کاربر بخواه فایل‌های پچ را در چت بگذارد (یا محتوایشان را).
- فقط به همین فایل‌های پچ اعتماد کن؛ هیچ کد دیگری از `IGBZ-pro` نگیر.

### ۳. اعمال پچ‌ها به ترتیب (⚠️ ترتیب مهم است)
```bash
cd /home/.../IGBZ-NopCommerce     # کلونِ گام ۱
for p in fix-round1 phase1-payir-checklist phase3-virtual-tryon phase3-tryon-quality-tips phase4-manus-manychat-guide phase2-bnpl checklist-admin-action; do
  git apply --check "/tmp/igbz-pro-src/igbz-nopcommerce-$p.patch" && echo "✓ check $p" || { echo "✗ FAIL check $p"; break; }
done
```
اگر همهٔ چک‌ها OK بود، اعمال کن:
```bash
for p in fix-round1 phase1-payir-checklist phase3-virtual-tryon phase3-tryon-quality-tips phase4-manus-manychat-guide phase2-bnpl checklist-admin-action; do
  git apply "/tmp/igbz-pro-src/igbz-nopcommerce-$p.patch" && echo "✓ $p"
done
git status --short        # باید ~۱۹ فایل (جدید + تغییر) باشد
```
> نکته: فایل `phase2-bnpl` شامل «حذف SnappPayBnplGateway قدیمی» هم هست — اگر در هر مرحله خطای apply دیدی، دقیق‌تر بررسی کن (ممکن است origin/main قبلاً بعضی را اعمال کرده باشد؛ در آن صورت فقط پچ‌های باقی‌مانده را اعمال کن).

### ۴. بررسی‌های ایستا (قبل از کامیت)
- سورس nopCommerce در این ریپو نیست و NuGet در دسترس نیست؛ پس `dotnet build` واقعی ممکن نیست — این را در گزارش نهایی صادقانه بنویس.
- حداقل چک کن:
  - هیچ بلوک `}` + `{` بی‌سربرگ در `Plugins/**/*.cs` نمانده (به‌خصوص `TenantPlanService.cs`).
  - `TrialEndDateUtc` از نوع `DateTime?` است.
  - فایل‌های جدید موجودند: `Domain/PhoneOtpCode.cs`, `Domain/Bnpl.cs`, `Domain/LaunchChecklistItemState.cs`, `Services/BnplService.cs`, `Services/VirtualTryOnService.cs`, `Services/LaunchChecklistService.cs`, `Controllers/Public/BnplController.cs`, `Controllers/Public/VirtualTryOnController.cs`, `Controllers/Admin/BnplAdminController.cs`, `Controllers/Admin/LaunchChecklistController.cs`, `Components/LaunchChecklistViewComponent.cs`, `Views/BnplAdmin/*`, `Views/Shared/Components/LaunchChecklist/*`.

### ۵. کامیت و پوش
```bash
git add -A
git commit -m "feat: فازهای ۱ تا ۴ — pay.ir، BNPL (دیجی‌پی/اسنپ‌پی)، پرو لباس IDM-VTON، چک‌لیست «فروشگاه‌ت رو بترکون» + اصلاحات دور اول"
git push origin main
```

### ۶. تأیید نهایی و گزارش
- بعد از پوش، تأیید کن: `git log origin/main -1` و `gh api repos/paymanshafayan/IGBZ-NopCommerce/commits/HEAD --jq .sha`.
- گزارش کوتاه بده: چه پچ‌هایی اعمال شد، وضعیت push، و این نکته که build واقعی باید روی سیستم کاربر (با سورس nopCommerce 4.90.6) اجرا شود.

## قواعد مهم
- **هیچ تغییر دیگری روی کد نده** — فقط همین پچ‌ها را به ترتیب اعمال و پوش کن (فقط در صورت خطای apply، کوچک‌ترین اصلاح ممکن).
- فایل‌های باینری/حجیم به ریپو اضافه نکن.
- بعد از پوش موفق، **متوقف شو و منتظر دستور کاربر بمان**.
