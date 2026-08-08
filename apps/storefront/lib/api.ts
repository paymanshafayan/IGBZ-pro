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
