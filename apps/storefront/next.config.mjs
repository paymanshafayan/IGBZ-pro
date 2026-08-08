/** @type {import('next').NextConfig} */
const nextConfig = {
  // در توسعه، درخواست‌های /api به بک‌اند IGBZ پراکسی می‌شوند
  async rewrites() {
    const apiBase = process.env.IGBZ_API_BASE ?? "http://localhost:5000";
    return [
      {
        source: "/api/:path*",
        destination: `${apiBase}/api/:path*`
      }
    ];
  }
};

export default nextConfig;
