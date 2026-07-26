# IGBZ Architecture Decisions

## ADR-001 — خروج کامل از nopCommerce

تصمیم: پروژه به صورت Native و مستقل ساخته می‌شود. nopCommerce فقط برای شناخت منطق و Edge Case مرجع مفهومی است و هیچ کدی از آن کپی نمی‌شود.

## ADR-002 — Backend با .NET 9 و C#

تصمیم: Solution با TargetFramework `net9.0` ایجاد شد.

## ADR-003 — MongoDB به عنوان دیتابیس اصلی

تصمیم: برای مدل سندی Product، Order، Variant، Integration و Reservation از MongoDB استفاده می‌شود.

## ADR-004 — معماری Modular Monolith

تصمیم: در شروع پروژه از Modular Monolith استفاده می‌شود تا پیچیدگی Microservice زودهنگام وارد پروژه نشود. مرزبندی‌ها در لایه Domain/Application حفظ شده‌اند.

## ADR-005 — Multi-Tenancy اجباری

تصمیم: داده‌های Tenant از طریق `ITenantScopedRepository<T>` خوانده/نوشته می‌شوند و Repository همیشه فیلتر `TenantId` را اعمال می‌کند.

## ADR-006 — موتور تخفیف MVP سطح ۲

تصمیم اجرایی اولیه: درصدی/مبلغ ثابت + کوپن، بدون ترکیب هم‌زمان چند تخفیف. طراحی به‌گونه‌ای است که بعداً قوانین ترکیب و اولویت اضافه شود.

## ADR-007 — پرداخت اولیه Mock Gateway

تصمیم: درگاه تستی `MockPaymentGatewayService` اضافه شده تا جریان PaymentIntent/Callback/Idempotency پیاده‌سازی شود. درگاه‌های واقعی بعداً پشت همین اینترفیس اضافه می‌شوند.

## ADR-008 — Auth اولیه با OTP و JWT

تصمیم: برای شروع، ورود passwordless مبتنی بر OTP و JWT پیاده‌سازی شد. در Development کد OTP در response برمی‌گردد، اما Production باید SMS Provider واقعی داشته باشد.

## ADR-009 — Job Queue اولیه Mongo-backed

تصمیم: برای فاز اولیه Integration، صف Job روی MongoDB ساخته شد تا RabbitMQ/Hangfire زودهنگام وارد پروژه نشود. در صورت رشد بار، همین abstraction می‌تواند به RabbitMQ/MassTransit منتقل شود.

## ADR-010 — OpenAPI داخلی .NET

تصمیم: برای مستندسازی ماشینی API از OpenAPI داخلی ASP.NET Core استفاده شد. در Development مسیر `/openapi/v1.json` فعال است.
