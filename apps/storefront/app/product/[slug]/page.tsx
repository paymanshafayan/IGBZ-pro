"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { getProduct, type ProductDetailDto } from "@/lib/api";
import { addToCart } from "@/lib/cart";

export default function ProductPage() {
  const { slug } = useParams<{ slug: string }>();

  const [product, setProduct] = useState<ProductDetailDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [selectedSku, setSelectedSku] = useState<string>("");
  const [quantity, setQuantity] = useState(1);
  const [added, setAdded] = useState(false);

  useEffect(() => {
    let cancelled = false;
    getProduct(slug)
      .then((p) => {
        if (cancelled) return;
        setProduct(p);
        setSelectedSku(p.variants[0]?.sku ?? "");
      })
      .catch((e) => {
        if (!cancelled) setError(e instanceof Error ? e.message : "خطا");
      });
    return () => {
      cancelled = true;
    };
  }, [slug]);

  if (error) {
    return <div className="alert alert-error">{error}</div>;
  }

  if (!product) {
    return <div className="loading">در حال بارگذاری…</div>;
  }

  const selectedVariant = product.variants.find((v) => v.sku === selectedSku) ?? product.variants[0];

  const handleAdd = () => {
    if (!selectedVariant) return;
    addToCart({
      productId: product.id,
      slug: product.slug,
      name: product.name,
      sku: selectedVariant.sku,
      imageUrl: product.images[0] ?? null,
      priceToman: selectedVariant.priceToman,
      quantity
    });
    setAdded(true);
    setTimeout(() => setAdded(false), 1500);
    window.dispatchEvent(new Event("cart-changed"));
  };

  return (
    <div className="product-detail">
      <div className="gallery">
        {product.images[0] ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img src={product.images[0]} alt={product.name} />
        ) : (
          <div style={{ height: 400, background: "#f0f0f0", borderRadius: 12 }} />
        )}
      </div>

      <div className="meta">
        <h1>{product.name}</h1>
        {product.description && <p>{product.description}</p>}

        {selectedVariant && (
          <div className="price-row">
            <span className="price" style={{ fontSize: "1.4rem", fontWeight: 800, color: "#5b1fd4" }}>
              {selectedVariant.priceToman.toLocaleString("fa-IR")} تومان
            </span>
            {selectedVariant.oldPriceToman != null && selectedVariant.oldPriceToman > selectedVariant.priceToman && (
              <span className="old-price">{selectedVariant.oldPriceToman.toLocaleString("fa-IR")} تومان</span>
            )}
          </div>
        )}

        {product.variants.length > 1 && (
          <div className="variant-select">
            <label>انتخاب مدل: </label>
            <select value={selectedSku} onChange={(e) => setSelectedSku(e.target.value)}>
              {product.variants.map((v) => (
                <option key={v.sku} value={v.sku}>
                  {v.attributes["color"] ?? v.attributes["size"] ?? v.sku}
                </option>
              ))}
            </select>
          </div>
        )}

        <div className="qty-row">
          <label>تعداد: </label>
          <input
            type="number"
            min={1}
            value={quantity}
            onChange={(e) => setQuantity(Math.max(1, parseInt(e.target.value) || 1))}
          />
        </div>

        <button className="btn btn-primary btn-block" onClick={handleAdd} disabled={!selectedVariant}>
          افزودن به سبد خرید
        </button>

        {added && <div className="alert alert-success">به سبد خرید اضافه شد ✓</div>}
      </div>
    </div>
  );
}
