import { JWT_EXPIRY_SKEW_SECONDS } from "@/lib/auth/cookies";

export function decodeJwtExpiry(token: string): Date | null {
  const segments = token.split(".");
  if (segments.length !== 3 || !segments[1]) {
    return null;
  }

  try {
    const payload = JSON.parse(fromBase64Url(segments[1])) as { exp?: unknown };
    if (typeof payload.exp !== "number" || !Number.isFinite(payload.exp)) {
      return null;
    }

    return new Date(payload.exp * 1000);
  } catch {
    return null;
  }
}

export function isJwtExpired(
  token: string,
  nowMs: number = Date.now(),
  skewSeconds: number = JWT_EXPIRY_SKEW_SECONDS,
): boolean {
  const expiry = decodeJwtExpiry(token);
  if (!expiry) {
    return true;
  }

  return nowMs >= expiry.getTime() - skewSeconds * 1000;
}

function fromBase64Url(value: string): string {
  const padded = value.replace(/-/g, "+").replace(/_/g, "/").padEnd(Math.ceil(value.length / 4) * 4, "=");
  return decodeUtf8(padded);
}

function decodeUtf8(base64: string): string {
  if (typeof atob === "function") {
    return atob(base64);
  }

  return Buffer.from(base64, "base64").toString("utf8");
}
