import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  distDir: "generated",
  turbopack: { root: process.cwd() },
  async redirects() {
    return [
      { source: "/operation/results", destination: "/records/result-semester", permanent: true },
      { source: "/record/results", destination: "/records/result-semester", permanent: true },
      { source: "/records/results", destination: "/records/result-semester", permanent: true },
    ];
  },
};

export default nextConfig;
