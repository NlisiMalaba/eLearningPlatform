import { NextRequest, NextResponse } from "next/server";
import { applyAuthCookies, clearAuthCookies } from "@/lib/auth/sessionCookies";
import { REFRESH_TOKEN_COOKIE } from "@/lib/auth/cookies";
import { parseTokenPair, postUpstreamJson, problemFromUpstream } from "@/lib/auth/upstreamAuth";

export async function POST(request: NextRequest): Promise<NextResponse> {
  const refreshToken = request.cookies.get(REFRESH_TOKEN_COOKIE)?.value;
  if (!refreshToken) {
    const response = NextResponse.json(
      {
        type: "https://eduzim.co.zw/errors/unauthenticated",
        title: "Session expired",
        status: 401,
        detail: "Sign in required.",
      },
      { status: 401 },
    );
    clearAuthCookies(response);
    return response;
  }

  const upstream = await postUpstreamJson("/api/v1/auth/refresh", { refreshToken });
  const tokens = parseTokenPair(upstream.body);
  if (!tokens || upstream.status !== 200) {
    const response = NextResponse.json(problemFromUpstream(upstream, "Session expired"), {
      status: 401,
    });
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
