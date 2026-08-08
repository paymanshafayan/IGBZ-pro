import Link from "next/link";
import { getProducts } from "@/lib/api";

export const dynamic = "force-dynamic";

export default async function HomePage() {
  let products = [];
  let error: string | null = null;

  try {
    products = await getProducts();
  } catch (e) {
    error = e instanceof Error ? e.message : "خطا در دریافت محصولات";
  }

  return (
    <>
      <h1 className="page-title">محصولات</h1>

      {error && <div className="alert alert-error">{error}</div>}

      {!error && products.length === 0 && (
        <div className="empty">هنوز محصولی منتشر نشده است.</div>
      )}

      <div className="product-grid">
        {products.map((p) => (
          <Link key={p.id} href={`/product/${p.slug}`} className="product-card">
            {p.imageUrl ? (
              // eslint-disable-next-line @next/next/no-img-element
              <img src={p.imageUrl} alt={p.name} />
            ) : (
              <div style={{ height: 200, background: "#f0f0f0" }} />
            )}
            <div className="info">
              <span className="name">{p.name}</span>
              <span className="price">{p.priceToman.toLocaleString("fa-IR")} تومان</span>
              {p.oldPriceToman != null && p.oldPriceToman > p.priceToman && (
                <span className="old-price">{p.oldPriceToman.toLocaleString("fa-IR")} تومان</span>
              )}
            </div>
          </Link>
        ))}
      </div>
    </>
  );
}
