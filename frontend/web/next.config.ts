import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  distDir: "generated",
  turbopack: { root: process.cwd() },
};

export default nextConfig;
