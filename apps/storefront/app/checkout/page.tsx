"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { getCart, clearCart, cartTotal, type CartLine } from "@/lib/cart";
import { createOrder, getStoredCustomerId } from "@/lib/api";
import Link from "next/link";

interface CheckoutState {
  status: "idle" | "submitting" | "success" | "error";
  orderId?: string;
  grandTotal?: number;
  message?: string;
}

export default function CheckoutPage() {
  const [cart, setCart] = useState<CartLine[]>([]);
  const [customerId, setCustomerId] = useState("");
  const [state, setState] = useState<CheckoutState>({ status: "idle" });

  useEffect(() => {
    setCart(getCart());
    setCustomerId(getStoredCustomerId() ?? "");
  }, []);

  const total = cartTotal(cart);

  const handleSubmit = async () => {
    if (cart.length === 0) return;

    setState({ status: "submitting" });

    try {
      const result = await createOrder({
        // مشتری واردشده → customerId واقعی؛ مهمان → "guest"
        customerId: customerId.trim() || "guest",
        lines: cart.map((c) => ({
          productId: c.productId,
          sku: c.sku,
          quantity: c.quantity
        })),
        taxRatePercent: 9
      });

      clearCart();
      setState({
        status: "success",
        orderId: result.orderId,
        grandTotal: result.grandTotalToman
      });
    } catch (e) {
      setState({
        status: "error",
        message: e instanceof Error ? e.message : "خطا در ثبت سفارش"
      });
    }
  };

  if (state.status === "success") {
    return (
      <div style={{ textAlign: "center", padding: "3rem 0" }}>
        <h1 className="page-title">✅ سفارش ثبت شد</h1>
        <div className="alert alert-success" style={{ display: "inline-block" }}>
          شناسهٔ سفارش: <b>{state.orderId}</b>
          <br />
          مبلغ کل: <b>{state.grandTotal?.toLocaleString("fa-IR")} تومان</b>
        </div>
        <p style={{ marginTop: "1rem", color: "#6b7280" }}>
          موجودی محصولات رزرو شد. اتصال پرداخت آنلاین در مرحلهٔ بعدی فعال می‌شود.
        </p>
        <Link href="/" className="btn btn-primary" style={{ marginTop: "1.5rem" }}>
          بازگشت به فروشگاه
        </Link>
      </div>
    );
  }

  return (
    <>
      <h1 className="page-title">تسویه حساب</h1>

      {cart.length === 0 && state.status !== "error" ? (
        <div className="empty">
          <p>سبد خرید شما خالی است.</p>
          <Link href="/" className="btn btn-primary" style={{ marginTop: "1rem" }}>
            مشاهدهٔ محصولات
          </Link>
        </div>
      ) : (
        <>
          <table className="cart-table">
            <thead>
              <tr>
                <th>محصول</th>
                <th>تعداد</th>
                <th>جمع</th>
              </tr>
            </thead>
            <tbody>
              {cart.map((line) => (
                <tr key={line.sku}>
                  <td>{line.name}</td>
                  <td>{line.quantity}</td>
                  <td>{(line.priceToman * line.quantity).toLocaleString("fa-IR")}</td>
                </tr>
              ))}
            </tbody>
          </table>

          <div className="cart-summary">
            <p style={{ fontSize: "1.2rem", fontWeight: 800 }}>
              جمع کل: {total.toLocaleString("fa-IR")} تومان
            </p>
          </div>

          <div style={{ margin: "1.5rem 0", maxWidth: 480 }}>
            <label style={{ display: "block", marginBottom: "0.4rem" }}>شناسهٔ مشتری (اختیاری)</label>
            <input
              value={customerId}
              onChange={(e) => setCustomerId(e.target.value)}
              placeholder="guest"
              style={{ width: "100%", padding: "0.7rem", borderRadius: 10, border: "1px solid #e5e7eb" }}
            />
          </div>

          {state.status === "error" && <div className="alert alert-error">{state.message}</div>}

          <button
            className="btn btn-primary"
            onClick={handleSubmit}
            disabled={state.status === "submitting"}
            style={{ minWidth: 200 }}
          >
            {state.status === "submitting" ? "در حال ثبت…" : "ثبت سفارش"}
          </button>
        </>
      )}
    </>
  );
}
