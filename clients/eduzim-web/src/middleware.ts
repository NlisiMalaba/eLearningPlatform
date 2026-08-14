import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
import { ACCESS_TOKEN_COOKIE, REFRESH_TOKEN_COOKIE } from "@/lib/auth/cookies";
import { decideAuthGate } from "@/lib/auth/authGate";
import { applyAuthCookies, clearAuthCookies } from "@/lib/auth/sessionCookies";
import { parseTokenPair, postUpstreamJson } from "@/lib/auth/upstreamAuth";
import { safeRedirectPath } from "@/lib/auth/safeRedirect";

export async function middleware(request: NextRequest): Promise<NextResponse> {
  const { pathname } = request.nextUrl;
  const accessToken = request.cookies.get(ACCESS_TOKEN_COOKIE)?.value;
  const refreshToken = request.cookies.get(REFRESH_TOKEN_COOKIE)?.value;
  const decision = decideAuthGate({ pathname, accessToken, refreshToken });

  if (decision.type === "allow") {
    return NextResponse.next();
  }

  if (decision.type === "home") {
    return redirectHome(request);
  }

  if (decision.type === "login") {
    return redirectLogin(request);
  }

  const tokens = refreshToken ? await refreshFromUpstream(refreshToken) : null;
  if (!tokens) {
    if (decision.onward === "home") {
      const response = NextResponse.next();
      clearAuthCookies(response);
      return response;
    }

    return redirectLogin(request);
  }

  const response =
    decision.onward === "home" ? redirectHome(request) : NextResponse.next();
  applyAuthCookies(response, tokens);
  return response;
}

export const config = {
  matcher: ["/((?!_next/static|_next/image|.*\\.(?:svg|png|jpg|jpeg|gif|webp|ico)$).*)"],
};

async function refreshFromUpstream(refreshToken: string) {
  const upstream = await postUpstreamJson("/api/v1/auth/refresh", { refreshToken });
  if (upstream.status !== 200) {
    return null;
  }

  return parseTokenPair(upstream.body);
}

function redirectHome(request: NextRequest): NextResponse {
  const url = request.nextUrl.clone();
  url.pathname = "/";
  url.search = "";
  return NextResponse.redirect(url);
}

function redirectLogin(request: NextRequest): NextResponse {
  const url = request.nextUrl.clone();
  url.pathname = "/login";
  url.search = "";
  url.searchParams.set("from", safeRedirectPath(request.nextUrl.pathname));
  const response = NextResponse.redirect(url);
  clearAuthCookies(response);
  return response;
}
