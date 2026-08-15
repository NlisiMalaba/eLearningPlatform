import { randomUUID } from "node:crypto";
import { spawnSync } from "node:child_process";
import withSerwistInit from "@serwist/next";
import type { NextConfig } from "next";

const revision =
  spawnSync("git", ["rev-parse", "HEAD"], { encoding: "utf-8" }).stdout.trim() ||
  randomUUID();

const withSerwist = withSerwistInit({
  additionalPrecacheEntries: [{ url: "/~offline", revision }],
  disable: process.env.NODE_ENV === "development",
  swSrc: "src/app/sw.ts",
  swDest: "public/sw.js",
});

const apiBase =
  process.env.EDUZIM_API_URL?.trim().replace(/\/$/, "") || "http://localhost:5196";

const nextConfig: NextConfig = {
  reactStrictMode: true,
  transpilePackages: ["three"],
  async rewrites() {
    return [
      {
        source: "/hubs/:path*",
        destination: `${apiBase}/hubs/:path*`,
      },
    ];
  },
};

export default withSerwist(nextConfig);
