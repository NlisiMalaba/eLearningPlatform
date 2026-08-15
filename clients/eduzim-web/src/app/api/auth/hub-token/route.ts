import { NextRequest, NextResponse } from "next/server";
import { ACCESS_TOKEN_COOKIE, REFRESH_TOKEN_COOKIE } from "@/lib/auth/cookies";
import { applyAuthCookies, clearAuthCookies } from "@/lib/auth/sessionCookies";
import { isJwtExpired } from "@/lib/auth/jwt";
import { parseTokenPair, postUpstreamJson } from "@/lib/auth/upstreamAuth";

export async function GET(request: NextRequest): Promise<NextResponse> {
  const accessToken = request.cookies.get(ACCESS_TOKEN_COOKIE)?.value;
  const refreshToken = request.cookies.get(REFRESH_TOKEN_COOKIE)?.value;

  if (accessToken && !isJwtExpired(accessToken)) {
    return NextResponse.json({ accessToken });
  }

  if (!refreshToken) {
    const response = NextResponse.json({ accessToken: null }, { status: 401 });
    clearAuthCookies(response);
    return response;
  }

  const upstream = await postUpstreamJson("/api/v1/auth/refresh", { refreshToken });
  const tokens = parseTokenPair(upstream.body);
  if (!tokens) {
    const response = NextResponse.json({ accessToken: null }, { status: 401 });
    clearAuthCookies(response);
    return response;
  }

  const response = NextResponse.json({ accessToken: tokens.accessToken });
  applyAuthCookies(response, tokens);
  return response;
}
