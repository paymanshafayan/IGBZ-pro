"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { getCart, updateQuantity, removeLine, cartTotal, type CartLine } from "@/lib/cart";

export default function CartPage() {
  const [cart, setCart] = useState<CartLine[]>([]);

  useEffect(() => {
    const sync = () => setCart([...getCart()]);
    sync();
    window.addEventListener("cart-changed", sync);
    return () => window.removeEventListener("cart-changed", sync);
  }, []);

  const total = cartTotal(cart);

  return (
    <>
      <h1 className="page-title">سبد خرید</h1>

      {cart.length === 0 ? (
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
                <th>قیمت</th>
                <th>تعداد</th>
                <th>جمع</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {cart.map((line) => (
                <tr key={line.sku}>
                  <td>{line.name}</td>
                  <td>{line.priceToman.toLocaleString("fa-IR")}</td>
                  <td>
                    <input
                      type="number"
                      min={1}
                      value={line.quantity}
                      style={{ width: 70, padding: "0.4rem", borderRadius: 8, border: "1px solid #e5e7eb" }}
                      onChange={(e) => setCart(updateQuantity(line.sku, parseInt(e.target.value) || 1))}
                    />
                  </td>
                  <td>{(line.priceToman * line.quantity).toLocaleString("fa-IR")}</td>
                  <td>
                    <button className="btn btn-outline" onClick={() => setCart(removeLine(line.sku))}>
                      حذف
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          <div className="cart-summary">
            <p style={{ fontSize: "1.2rem", fontWeight: 800 }}>
              جمع کل: {total.toLocaleString("fa-IR")} تومان
            </p>
            <Link href="/checkout" className="btn btn-primary btn-block" style={{ marginTop: "1rem" }}>
              ادامهٔ خرید (تسویه)
            </Link>
          </div>
        </>
      )}
    </>
  );
}
