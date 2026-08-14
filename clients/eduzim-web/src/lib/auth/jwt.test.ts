import { describe, expect, it } from "vitest";
import { decodeJwtExpiry, isJwtExpired } from "@/lib/auth/jwt";

function jwtWithExp(expSeconds: number): string {
  const payload = Buffer.from(JSON.stringify({ exp: expSeconds }), "utf8").toString("base64url");
  return `header.${payload}.signature`;
}

describe("decodeJwtExpiry", () => {
  it("reads exp from a JWT payload", () => {
    const exp = 1_800_000_000;
    const expiry = decodeJwtExpiry(jwtWithExp(exp));
    expect(expiry?.toISOString()).toBe(new Date(exp * 1000).toISOString());
  });

  it("returns null for malformed tokens", () => {
    expect(decodeJwtExpiry("not-a-jwt")).toBeNull();
  });
});

describe("isJwtExpired", () => {
  it("treats a token as expired at exp minus skew", () => {
    const nowMs = 1_700_000_000_000;
    const expSeconds = Math.floor(nowMs / 1000) + 10;
    expect(isJwtExpired(jwtWithExp(expSeconds), nowMs, 30)).toBe(true);
  });

  it("treats a future token as valid", () => {
    const nowMs = 1_700_000_000_000;
    const expSeconds = Math.floor(nowMs / 1000) + 120;
    expect(isJwtExpired(jwtWithExp(expSeconds), nowMs, 30)).toBe(false);
  });
});
