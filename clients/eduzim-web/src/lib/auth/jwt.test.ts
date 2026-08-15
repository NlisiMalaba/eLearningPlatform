import { describe, expect, it } from "vitest";
import { decodeJwtExpiry, isJwtExpired, readJwtRole, readJwtTenantId, readJwtUserId } from "@/lib/auth/jwt";

function jwtWithPayload(payload: Record<string, unknown>): string {
  const encoded = Buffer.from(JSON.stringify(payload), "utf8").toString("base64url");
  return `header.${encoded}.signature`;
}

function jwtWithExp(expSeconds: number): string {
  return jwtWithPayload({ exp: expSeconds });
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

describe("JWT identity claims", () => {
  it("reads user id and role from nameid and role claims", () => {
    const userId = "11111111-1111-1111-1111-111111111111";
    const token = jwtWithPayload({ nameid: userId, role: "Student", exp: 1_800_000_000 });
    expect(readJwtUserId(token)).toBe(userId);
    expect(readJwtRole(token)).toBe("Student");
  });

  it("reads tenant_id when present", () => {
    const tenantId = "22222222-2222-2222-2222-222222222222";
    const token = jwtWithPayload({ tenant_id: tenantId, exp: 1_800_000_000 });
    expect(readJwtTenantId(token)).toBe(tenantId);
  });
});
