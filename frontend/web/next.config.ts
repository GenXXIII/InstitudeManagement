import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  distDir: "generated",
  turbopack: { root: process.cwd() },
  async redirects() {
    return [
      { source: "/operation/results", destination: "/record-history/results", permanent: true },
      { source: "/record/results", destination: "/record-history/results", permanent: true },
      { source: "/records/results", destination: "/record-history/results", permanent: true },
    ];
  },
};

export default nextConfig;
