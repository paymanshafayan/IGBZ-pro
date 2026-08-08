// ─────────────────────────────────────────────────────────────
// سبد خرید — ذخیره در localStorage مرورگر
// ─────────────────────────────────────────────────────────────

export interface CartLine {
  productId: string;
  slug: string;
  name: string;
  sku: string;
  imageUrl: string | null;
  priceToman: number;
  quantity: number;
}

const CART_KEY = "igbz-cart";

export function getCart(): CartLine[] {
  if (typeof window === "undefined") return [];
  try {
    const raw = window.localStorage.getItem(CART_KEY);
    return raw ? (JSON.parse(raw) as CartLine[]) : [];
  } catch {
    return [];
  }
}

export function addToCart(line: CartLine): CartLine[] {
  const cart = getCart();
  const existing = cart.find((c) => c.sku === line.sku);
  if (existing) {
    existing.quantity += line.quantity;
  } else {
    cart.push(line);
  }
  saveCart(cart);
  return cart;
}

export function updateQuantity(sku: string, quantity: number): CartLine[] {
  const cart = getCart();
  const line = cart.find((c) => c.sku === sku);
  if (line) {
    if (quantity <= 0) {
      return removeLine(sku);
    }
    line.quantity = quantity;
  }
  saveCart(cart);
  return cart;
}

export function removeLine(sku: string): CartLine[] {
  const cart = getCart().filter((c) => c.sku !== sku);
  saveCart(cart);
  return cart;
}

export function clearCart(): void {
  if (typeof window !== "undefined") {
    window.localStorage.removeItem(CART_KEY);
  }
}

export function cartTotal(cart: CartLine[]): number {
  return cart.reduce((sum, c) => sum + c.priceToman * c.quantity, 0);
}

function saveCart(cart: CartLine[]): void {
  if (typeof window !== "undefined") {
    window.localStorage.setItem(CART_KEY, JSON.stringify(cart));
  }
}
