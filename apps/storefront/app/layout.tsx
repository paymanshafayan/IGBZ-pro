import type { Metadata } from "next";
import Link from "next/link";
import "./globals.css";

export const metadata: Metadata = {
  title: "فروشگاه من",
  description: "فروشگاه اینترنتی اختصاصی شما"
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="fa" dir="rtl">
      <body>
        <header className="site-header">
          <div className="container">
            <Link href="/" className="brand">
              🛍️ فروشگاه من
            </Link>
            <nav className="nav-links">
              <Link href="/">محصولات</Link>
              <Link href="/cart">
                سبد خرید <span className="cart-badge" id="cart-count">۰</span>
              </Link>
            </nav>
          </div>
        </header>
        <main className="container">{children}</main>
      </body>
    </html>
  );
}
