"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { loginCustomer, registerCustomer } from "@/lib/api";

export default function LoginPage() {
  const router = useRouter();
  const [mode, setMode] = useState<"login" | "register">("login");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [phone, setPhone] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const handleSubmit = async () => {
    if (!email || !password) {
      setError("ایمیل و رمز عبور الزامی است.");
      return;
    }
    setBusy(true);
    setError(null);

    const result =
      mode === "login"
        ? await loginCustomer(email, password)
        : await registerCustomer(email, password, phone || undefined);

    setBusy(false);

    if (!result.success) {
      setError(result.message ?? "خطا");
      return;
    }

    router.push("/");
    router.refresh();
  };

  return (
    <div style={{ maxWidth: 420, margin: "3rem auto" }}>
      <h1 className="page-title" style={{ textAlign: "center" }}>
        {mode === "login" ? "ورود به حساب" : "ثبت‌نام"}
      </h1>

      <div style={{ display: "flex", gap: "0.5rem", marginBottom: "1.2rem" }}>
        <button
          className={`btn ${mode === "login" ? "btn-primary" : "btn-outline"}`}
          style={{ flex: 1 }}
          onClick={() => setMode("login")}
        >
          ورود
        </button>
        <button
          className={`btn ${mode === "register" ? "btn-primary" : "btn-outline"}`}
          style={{ flex: 1 }}
          onClick={() => setMode("register")}
        >
          ثبت‌نام
        </button>
      </div>

      <div style={{ display: "grid", gap: "0.9rem" }}>
        <input
          type="email"
          placeholder="ایمیل"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          style={{ padding: "0.7rem", borderRadius: 10, border: "1px solid #e5e7eb", fontSize: "1rem" }}
        />
        <input
          type="password"
          placeholder="رمز عبور"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          style={{ padding: "0.7rem", borderRadius: 10, border: "1px solid #e5e7eb", fontSize: "1rem" }}
        />
        {mode === "register" && (
          <input
            type="tel"
            placeholder="شمارهٔ موبایل (اختیاری)"
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
            style={{ padding: "0.7rem", borderRadius: 10, border: "1px solid #e5e7eb", fontSize: "1rem" }}
          />
        )}
      </div>

      {error && <div className="alert alert-error" style={{ marginTop: "1rem" }}>{error}</div>}

      <button
        className="btn btn-primary btn-block"
        style={{ marginTop: "1.2rem" }}
        onClick={handleSubmit}
        disabled={busy}
      >
        {busy ? "در حال ارسال…" : mode === "login" ? "ورود" : "ثبت‌نام"}
      </button>

      <p style={{ textAlign: "center", marginTop: "1rem", color: "#6b7280" }}>
        <Link href="/">← بازگشت به فروشگاه</Link>
      </p>
    </div>
  );
}
