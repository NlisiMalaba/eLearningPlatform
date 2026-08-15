import { NextResponse } from "next/server";
import {
  ACCESS_TOKEN_COOKIE,
  AUTH_COOKIE_PATH,
  REFRESH_TOKEN_COOKIE,
} from "@/lib/auth/cookies";
import type { TokenPair } from "@/lib/auth/types";

export function applyAuthCookies(response: NextResponse, tokens: TokenPair): void {
  setTokenCookie(response, ACCESS_TOKEN_COOKIE, tokens.accessToken, tokens.accessTokenExpiresAt);
  setTokenCookie(response, REFRESH_TOKEN_COOKIE, tokens.refreshToken, tokens.refreshTokenExpiresAt);
}

export function clearAuthCookies(response: NextResponse): void {
  expireCookie(response, ACCESS_TOKEN_COOKIE);
  expireCookie(response, REFRESH_TOKEN_COOKIE);
}

export function secondsUntil(expiresAt: Date, nowMs: number = Date.now()): number {
  return Math.max(1, Math.floor((expiresAt.getTime() - nowMs) / 1000));
}

function setTokenCookie(
  response: NextResponse,
  name: string,
  value: string,
  expiresAt: Date,
): void {
  response.cookies.set({
    name,
    value,
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: AUTH_COOKIE_PATH,
    maxAge: secondsUntil(expiresAt),
  });
}

function expireCookie(response: NextResponse, name: string): void {
  response.cookies.set({
    name,
    value: "",
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: AUTH_COOKIE_PATH,
    maxAge: 0,
  });
}
