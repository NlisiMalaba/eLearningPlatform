import { NextRequest, NextResponse } from "next/server";
import { ACCESS_TOKEN_COOKIE, REFRESH_TOKEN_COOKIE } from "@/lib/auth/cookies";
import { applyAuthCookies, clearAuthCookies } from "@/lib/auth/sessionCookies";
import { decodeJwtExpiry, isJwtExpired } from "@/lib/auth/jwt";
import { parseTokenPair, postUpstreamJson } from "@/lib/auth/upstreamAuth";

export async function GET(request: NextRequest): Promise<NextResponse> {
  const accessToken = request.cookies.get(ACCESS_TOKEN_COOKIE)?.value;
  const refreshToken = request.cookies.get(REFRESH_TOKEN_COOKIE)?.value;

  if (accessToken && !isJwtExpired(accessToken)) {
    return NextResponse.json({
      authenticated: true,
      accessTokenExpiresAt: decodeJwtExpiry(accessToken)?.toISOString(),
    });
  }

  if (!refreshToken) {
    const response = NextResponse.json({ authenticated: false }, { status: 401 });
    clearAuthCookies(response);
    return response;
  }

  const upstream = await postUpstreamJson("/api/v1/auth/refresh", { refreshToken });
  const tokens = parseTokenPair(upstream.body);
  if (!tokens) {
    const response = NextResponse.json({ authenticated: false }, { status: 401 });
    clearAuthCookies(response);
    return response;
  }

  const response = NextResponse.json({
    authenticated: true,
    accessTokenExpiresAt: tokens.accessTokenExpiresAt.toISOString(),
  });
  applyAuthCookies(response, tokens);
  return response;
}
