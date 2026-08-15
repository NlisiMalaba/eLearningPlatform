import { describe, expect, it } from "vitest";
import { safeRedirectPath } from "@/lib/auth/safeRedirect";

describe("safeRedirectPath", () => {
  it("allows in-app relative paths", () => {
    expect(safeRedirectPath("/modules/abc")).toBe("/modules/abc");
  });

  it("rejects open redirects and auth loops", () => {
    expect(safeRedirectPath("https://evil.example")).toBe("/");
    expect(safeRedirectPath("//evil.example")).toBe("/");
    expect(safeRedirectPath("/login")).toBe("/");
    expect(safeRedirectPath(undefined)).toBe("/");
  });
});
