// ─────────────────────────────────────────────────────────────
// کلاینت API برای Storefront
// درخواست‌ها به /api/* می‌روند که در dev به بک‌اند IGBZ پراکسی می‌شود.
// تننت از هدر X-Tenant-Id (برای توسعهٔ محلی) یا Host مرورگر تشخیص داده می‌شود.
// ─────────────────────────────────────────────────────────────

export interface ProductListDto {
  id: string;
  slug: string;
  name: string;
  imageUrl: string | null;
  priceToman: number;
  oldPriceToman: number | null;
}

export interface ProductVariantDto {
  sku: string;
  attributes: Record<string, string>;
  priceToman: number;
  oldPriceToman: number | null;
  availableQuantity: number;
}

export interface ProductDetailDto {
  id: string;
  slug: string;
  name: string;
  description: string | null;
  images: string[];
  variants: ProductVariantDto[];
  isDigital: boolean;
}

const TENANT_HEADER = process.env.NEXT_PUBLIC_TENANT_ID
  ? { "X-Tenant-Id": process.env.NEXT_PUBLIC_TENANT_ID }
  : {};

// ─────────────────────────────────────────────────────────────
// توکن JWT مشتری — ذخیره در localStorage (کلاینت)
// ─────────────────────────────────────────────────────────────

const TOKEN_KEY = "igbz-token";
const CUSTOMER_ID_KEY = "igbz-customer-id";

export function getStoredToken(): string | null {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem(TOKEN_KEY);
}

export function getStoredCustomerId(): string | null {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem(CUSTOMER_ID_KEY);
}

export function storeSession(token: string, customerId: string): void {
  if (typeof window !== "undefined") {
    window.localStorage.setItem(TOKEN_KEY, token);
    window.localStorage.setItem(CUSTOMER_ID_KEY, customerId);
  }
}

export function clearSession(): void {
  if (typeof window !== "undefined") {
    window.localStorage.removeItem(TOKEN_KEY);
    window.localStorage.removeItem(CUSTOMER_ID_KEY);
  }
}

export interface AuthResult {
  success: boolean;
  accessToken?: string;
  customerId?: string;
  message?: string;
}

export async function registerCustomer(
  email: string,
  password: string,
  phone?: string
): Promise<AuthResult> {
  try {
    const res = await fetch(`/api/auth/register`, {
      method: "POST",
      headers: { "Content-Type": "application/json", ...TENANT_HEADER },
      body: JSON.stringify({ email, password, phone, isTenantOwner: false }),
      cache: "no-store"
    });
    const data = await res.json();
    if (!res.ok || !data.success) {
      return { success: false, message: data?.message ?? "خطا در ثبت‌نام" };
    }
    storeSession(data.accessToken, data.customerId);
    return { success: true, accessToken: data.accessToken, customerId: data.customerId };
  } catch {
    return { success: false, message: "خطای شبکه" };
  }
}

export async function loginCustomer(email: string, password: string): Promise<AuthResult> {
  try {
    const res = await fetch(`/api/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json", ...TENANT_HEADER },
      body: JSON.stringify({ email, password }),
      cache: "no-store"
    });
    const data = await res.json();
    if (!res.ok || !data.success) {
      return { success: false, message: data?.message ?? "خطا در ورود" };
    }
    storeSession(data.accessToken, data.customerId);
    return { success: true, accessToken: data.accessToken, customerId: data.customerId };
  } catch {
    return { success: false, message: "خطای شبکه" };
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`/api${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...TENANT_HEADER,
      ...(init?.headers ?? {})
    },
    cache: "no-store"
  });

  if (!res.ok) {
    let message = `خطا ${res.status}`;
    try {
      const body = await res.json();
      message = body?.message ?? message;
    } catch {
      // ignore
    }
    throw new Error(message);
  }

  return (await res.json()) as T;
}

export async function getProducts(): Promise<ProductListDto[]> {
  const data = await request<{ success: boolean; products: ProductListDto[] }>("/catalog/products");
  return data.products;
}

export async function getProduct(slug: string): Promise<ProductDetailDto> {
  const data = await request<{ success: boolean; product: ProductDetailDto }>(
    `/catalog/products/${encodeURIComponent(slug)}`
  );
  return data.product;
}

export interface CreateOrderPayload {
  customerId: string;
  couponCode?: string | null;
  shippingToman?: number;
  taxRatePercent?: number;
  lines: { productId: string; sku: string; quantity: number }[];
}

export interface CreateOrderResult {
  success: boolean;
  orderId: string;
  status: string;
  grandTotalToman: number;
  discountToman: number;
}

export async function createOrder(payload: CreateOrderPayload): Promise<CreateOrderResult> {
  return request<CreateOrderResult>("/orders", { method: "POST", body: JSON.stringify(payload) });
}

export interface PaymentRequestResult {
  success: boolean;
  trackingNumber: string | null;
  redirectUrl: string | null;
}

export async function requestPayment(callbackUrl: string): Promise<PaymentRequestResult> {
  return request<PaymentRequestResult>("/payments/request", {
    method: "POST",
    body: JSON.stringify({ gatewayName: "test", callbackUrl })
  });
}

export interface PaymentVerifyResult {
  success: boolean;
  alreadyProcessed: boolean;
  bankRefId: string | null;
  message?: string;
}

export async function verifyPayment(trackingNumber: string): Promise<PaymentVerifyResult> {
  return request<PaymentVerifyResult>("/payments/verify", {
    method: "POST",
    body: JSON.stringify({ trackingNumber })
  });
}
