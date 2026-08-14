import { describe, expect, it } from "vitest";
import { isAuthPath, isPublicPath } from "@/lib/auth/routes";

describe("isAuthPath", () => {
  it("matches authentication screens", () => {
    expect(isAuthPath("/login")).toBe(true);
    expect(isAuthPath("/register")).toBe(true);
    expect(isAuthPath("/verify-email")).toBe(true);
    expect(isAuthPath("/sso")).toBe(true);
    expect(isAuthPath("/sso/callback")).toBe(true);
  });

  it("does not match app screens", () => {
    expect(isAuthPath("/")).toBe(false);
    expect(isAuthPath("/modules")).toBe(false);
  });
});

describe("isPublicPath", () => {
  it("allows offline fallback, API, and PWA assets", () => {
    expect(isPublicPath("/~offline")).toBe(true);
    expect(isPublicPath("/api/v1/sync/upload")).toBe(true);
    expect(isPublicPath("/api/auth/login")).toBe(true);
    expect(isPublicPath("/sw.js")).toBe(true);
    expect(isPublicPath("/manifest.webmanifest")).toBe(true);
    expect(isPublicPath("/icons/icon-192")).toBe(true);
  });

  it("does not treat the home page as public", () => {
    expect(isPublicPath("/")).toBe(false);
  });
});
