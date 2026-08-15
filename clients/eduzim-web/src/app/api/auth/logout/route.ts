import { NextRequest, NextResponse } from "next/server";
import { REFRESH_TOKEN_COOKIE } from "@/lib/auth/cookies";
import { clearAuthCookies } from "@/lib/auth/sessionCookies";
import { postUpstreamJson } from "@/lib/auth/upstreamAuth";

export async function POST(request: NextRequest): Promise<NextResponse> {
  const refreshToken = request.cookies.get(REFRESH_TOKEN_COOKIE)?.value;
  if (refreshToken) {
    await postUpstreamJson("/api/v1/auth/logout", { refreshToken });
  }

  const response = new NextResponse(null, { status: 204 });
  clearAuthCookies(response);
  return response;
}
